using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using iTextSharp.text.pdf.security;

namespace EtaxSignUtil
{
    // custom IExternalSignature ที่เซ็น hash ผ่าน Windows native API (CAPI/CNG)
    // แทนการใช้ X509Certificate2Signature ของ iTextSharp ที่เรียก cert.PrivateKey ข้างใน
    // การ bypass .PrivateKey จำเป็นเพราะตั้งแต่ cumulative update ล่าสุด
    // .NET runtime reject cert ที่ key algorithm ไม่ตรงกับ expectation ของ managed layer
    internal sealed class NativeX509Signature : IExternalSignature, IDisposable
    {
        private readonly string HashAlgorithmName;   // เช่น "SHA-1", "SHA-256"
        private readonly string EncryptionAlgorithmName;

        private IntPtr KeyHandle = IntPtr.Zero;      // HCRYPTPROV (CSP) หรือ NCRYPT_KEY_HANDLE (CNG)
        private uint KeySpec = 0;
        private bool CallerFreeHandle = false;       // Windows บอกว่าต้อง free เองไหม
        private bool IsNCryptKey = false;            // true = CNG / false = CSP

        // สร้าง signer โดยใช้ native key handle ของ cert
        // pin: ถ้ามีจะถูก set เข้า provider/key ก่อนเซ็นทุกครั้ง (เหมือน cache PIN เดิม)
        public NativeX509Signature(X509Certificate2 cert, string hashAlgorithm, string pin)
        {
            if (cert == null)
                throw new ArgumentNullException("cert");
            if (string.IsNullOrEmpty(hashAlgorithm))
                throw new ArgumentException("hashAlgorithm is required", "hashAlgorithm");

            this.HashAlgorithmName = hashAlgorithm;
            this.EncryptionAlgorithmName = "RSA";

            // ขอ handle private key ผ่าน Windows API ตรง — ข้าม cert.PrivateKey ที่ตอนนี้ strict เกิน
            // ใช้ ALLOW_NCRYPT_KEY_FLAG เพื่อรองรับทั้ง cert CSP (ส่วนใหญ่ของ Token) + CNG (forward-compat)
            // ไม่ใส่ PREFER_NCRYPT เพื่อให้ Windows คืน handle ตามที่ cert ถูก enroll ไว้จริง ๆ
            uint flags = NativeCrypto.CRYPT_ACQUIRE_CACHE_FLAG | NativeCrypto.CRYPT_ACQUIRE_ALLOW_NCRYPT_KEY_FLAG;
            IntPtr hKey;
            uint keySpec;
            bool callerFree;
            bool ok = NativeCrypto.CryptAcquireCertificatePrivateKey(
                cert.Handle, flags, IntPtr.Zero,
                out hKey, out keySpec, out callerFree);
            if (!ok)
            {
                int err = Marshal.GetLastWin32Error();
                throw new CryptographicException(
                    string.Format("ไม่สามารถขอ private key ของ Certificate ได้ (error 0x{0:X8})", err));
            }

            this.KeyHandle = hKey;
            this.KeySpec = keySpec;
            this.CallerFreeHandle = callerFree;
            this.IsNCryptKey = (keySpec == NativeCrypto.CERT_NCRYPT_KEY_SPEC);

            // ตั้ง PIN (ถ้ามี) — ช่วยให้ sign ต่อเนื่องหลายใบไม่ต้องกรอก PIN ซ้ำ
            if (!string.IsNullOrEmpty(pin))
                TrySetPin(pin);
        }

        public string GetHashAlgorithm() { return HashAlgorithmName; }
        public string GetEncryptionAlgorithm() { return EncryptionAlgorithmName; }

        // เซ็น message — iTextSharp MakeSignature.SignDetached จะส่ง raw bytes มาให้
        // implementation ต้อง hash เอง แล้ว sign (เทียบเท่า RSACryptoServiceProvider.SignData)
        public byte[] Sign(byte[] message)
        {
            byte[] hash = NativeCrypto.ComputeHash(message, HashAlgorithmName);
            return IsNCryptKey ? SignWithCng(hash) : SignWithCsp(hash);
        }

        // เซ็นผ่าน CAPI/CSP — เส้นทางหลักสำหรับ Token ส่วนใหญ่ที่ลูกค้าใช้
        private byte[] SignWithCsp(byte[] hashValue)
        {
            IntPtr hHash = IntPtr.Zero;
            try
            {
                uint algId = NativeCrypto.MapHashAlgIdCapi(HashAlgorithmName);
                if (!NativeCrypto.CryptCreateHash(KeyHandle, algId, IntPtr.Zero, 0, out hHash))
                    throw Win32("CryptCreateHash");

                // ใส่ค่า hash ที่คำนวณแล้ว — ไม่ต้องให้ CSP hash ใหม่
                if (!NativeCrypto.CryptSetHashParam(hHash, NativeCrypto.HP_HASHVAL, hashValue, 0))
                    throw Win32("CryptSetHashParam");

                // round 1 ขอความยาว signature
                uint sigLen = 0;
                if (!NativeCrypto.CryptSignHash(hHash, KeySpec, IntPtr.Zero, 0, null, ref sigLen))
                {
                    // บาง Token key เก็บใน AT_SIGNATURE ต้องลองสลับ keySpec
                    if (KeySpec == NativeCrypto.AT_KEYEXCHANGE)
                    {
                        KeySpec = NativeCrypto.AT_SIGNATURE;
                        if (!NativeCrypto.CryptSignHash(hHash, KeySpec, IntPtr.Zero, 0, null, ref sigLen))
                            throw Win32("CryptSignHash (size)");
                    }
                    else throw Win32("CryptSignHash (size)");
                }

                // round 2 ได้ signature จริง
                byte[] signature = new byte[sigLen];
                if (!NativeCrypto.CryptSignHash(hHash, KeySpec, IntPtr.Zero, 0, signature, ref sigLen))
                    throw Win32("CryptSignHash");

                // CSP คืน signature เป็น little-endian ต้อง reverse ให้เป็น big-endian ตามมาตรฐาน PKCS#1/CMS
                Array.Reverse(signature);
                return signature;
            }
            finally
            {
                if (hHash != IntPtr.Zero)
                    NativeCrypto.CryptDestroyHash(hHash);
            }
        }

        // เซ็นผ่าน CNG — เส้นทางสำหรับ cert ที่ถูก enroll เป็น KSP (Token รุ่นใหม่หรือ cert ที่ migrate ไป CNG)
        private byte[] SignWithCng(byte[] hashValue)
        {
            NativeCrypto.BCRYPT_PKCS1_PADDING_INFO padInfo = new NativeCrypto.BCRYPT_PKCS1_PADDING_INFO();
            padInfo.pszAlgId = Marshal.StringToHGlobalUni(NativeCrypto.MapHashAlgIdCng(HashAlgorithmName));
            IntPtr padInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeCrypto.BCRYPT_PKCS1_PADDING_INFO)));
            try
            {
                Marshal.StructureToPtr(padInfo, padInfoPtr, false);

                // round 1 ขอความยาว signature
                int sigLen;
                int status = NativeCrypto.NCryptSignHash(
                    KeyHandle, padInfoPtr,
                    hashValue, hashValue.Length,
                    null, 0, out sigLen,
                    NativeCrypto.BCRYPT_PAD_PKCS1);
                if (status != 0)
                    throw NCrypt("NCryptSignHash (size)", status);

                // round 2 ได้ signature
                byte[] signature = new byte[sigLen];
                status = NativeCrypto.NCryptSignHash(
                    KeyHandle, padInfoPtr,
                    hashValue, hashValue.Length,
                    signature, sigLen, out sigLen,
                    NativeCrypto.BCRYPT_PAD_PKCS1);
                if (status != 0)
                    throw NCrypt("NCryptSignHash", status);

                // CNG คืน signature เป็น big-endian อยู่แล้ว ไม่ต้อง reverse
                if (sigLen != signature.Length)
                {
                    byte[] trimmed = new byte[sigLen];
                    Buffer.BlockCopy(signature, 0, trimmed, 0, sigLen);
                    signature = trimmed;
                }
                return signature;
            }
            finally
            {
                Marshal.FreeHGlobal(padInfoPtr);
                if (padInfo.pszAlgId != IntPtr.Zero)
                    Marshal.FreeHGlobal(padInfo.pszAlgId);
            }
        }

        // ตั้ง PIN เข้า handle — ทั้ง CSP และ CNG มี API คนละตัว
        // ถ้า fail ไม่ throw เพราะบาง Token อาจไม่รองรับการตั้ง PIN แบบ programmatic
        // (จะไปเด้ง UI ของ SafeNet แทน — ซึ่งก็ใช้งานได้อยู่แล้ว)
        private void TrySetPin(string pin)
        {
            try
            {
                if (IsNCryptKey)
                {
                    // CNG รับ PIN เป็น Unicode string พร้อม null-terminator
                    byte[] pinBytes = System.Text.Encoding.Unicode.GetBytes(pin + "\0");
                    NativeCrypto.NCryptSetProperty(
                        KeyHandle, NativeCrypto.NCRYPT_PIN_PROPERTY,
                        pinBytes, pinBytes.Length, 0);
                }
                else
                {
                    // CSP รับ PIN เป็น ANSI พร้อม null-terminator
                    byte[] pinBytes = System.Text.Encoding.ASCII.GetBytes(pin + "\0");
                    NativeCrypto.CryptSetProvParam(
                        KeyHandle, NativeCrypto.PP_SIGNATURE_PIN,
                        pinBytes, 0);
                    // set keyexchange pin ด้วยกรณี keyspec เป็น AT_KEYEXCHANGE
                    NativeCrypto.CryptSetProvParam(
                        KeyHandle, NativeCrypto.PP_KEYEXCHANGE_PIN,
                        pinBytes, 0);
                }
            }
            catch
            {
                // เงียบไว้ ถ้าตั้ง PIN ไม่ได้ก็ปล่อยให้ SafeNet UI เด้งถามเอง
            }
        }

        // รวม Win32 error เป็น CryptographicException ให้อ่านง่าย
        private static CryptographicException Win32(string method)
        {
            int err = Marshal.GetLastWin32Error();
            return new CryptographicException(
                string.Format("{0} ล้มเหลว (error 0x{1:X8})", method, err));
        }

        private static CryptographicException NCrypt(string method, int status)
        {
            return new CryptographicException(
                string.Format("{0} ล้มเหลว (NTSTATUS 0x{1:X8})", method, status));
        }

        public void Dispose()
        {
            if (CallerFreeHandle && KeyHandle != IntPtr.Zero)
            {
                NativeCrypto.ReleaseKeyHandle(KeyHandle, IsNCryptKey);
                KeyHandle = IntPtr.Zero;
            }
        }
    }
}
