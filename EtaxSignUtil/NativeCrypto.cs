using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace EtaxSignUtil
{
    // รวม P/Invoke สำหรับเข้าถึง private key ของ certificate ผ่าน Windows native API
    // เหตุผล: ตั้งแต่ Windows/.NET cumulative update ล่าสุด property `X509Certificate2.PrivateKey`
    // ถูกทำให้ strict ขึ้น reject cert บางชนิดด้วย error "The certificate key algorithm is not supported"
    // การเรียก Windows API ตรง ๆ จะข้าม validation ของ managed layer ไปได้
    // รองรับทั้ง CSP (legacy) และ CNG (Next Generation) — cert ของ Token ส่วนใหญ่ยังอยู่ CSP
    internal static class NativeCrypto
    {
        #region ค่าคงที่ (constants)

        // flags สำหรับ CryptAcquireCertificatePrivateKey
        public const uint CRYPT_ACQUIRE_CACHE_FLAG = 0x00000001;
        public const uint CRYPT_ACQUIRE_SILENT_FLAG = 0x00000040;
        public const uint CRYPT_ACQUIRE_ALLOW_NCRYPT_KEY_FLAG = 0x00010000;
        public const uint CRYPT_ACQUIRE_PREFER_NCRYPT_KEY_FLAG = 0x00020000;

        // keySpec ค่าพิเศษเมื่อ handle ที่คืนมาเป็น CNG (NCRYPT_KEY_HANDLE)
        public const uint CERT_NCRYPT_KEY_SPEC = 0xFFFFFFFF;
        public const uint AT_KEYEXCHANGE = 1;
        public const uint AT_SIGNATURE = 2;

        // hash algorithm IDs สำหรับ CAPI
        public const uint CALG_SHA1 = 0x00008004;
        public const uint CALG_SHA_256 = 0x0000800C;
        public const uint CALG_SHA_384 = 0x0000800D;
        public const uint CALG_SHA_512 = 0x0000800E;

        // parameter IDs สำหรับ CryptGetProvParam / CryptSetProvParam
        public const uint PP_CONTAINER = 6;
        public const uint PP_SIGNATURE_PIN = 33;      // ตั้ง PIN สำหรับ signing
        public const uint PP_KEYEXCHANGE_PIN = 32;

        // property ID ของ certificate context — ใช้อ่าน provider info ที่เก็บไว้ใน metadata ของ cert
        // เชื่อถือได้กว่า CryptGetProvParam(PP_NAME) เพราะบาง CSP driver (eToken) คืน container แทน provider
        public const uint CERT_KEY_PROV_INFO_PROP_ID = 2;

        // parameter IDs สำหรับ CryptGetHashParam / CryptSetHashParam
        public const uint HP_HASHVAL = 0x0002;

        // flags สำหรับ NCryptSignHash
        public const int BCRYPT_PAD_PKCS1 = 2;

        // ชื่อ property ของ CNG key
        public const string NCRYPT_PIN_PROPERTY = "SmartCardPin";
        public const string NCRYPT_NAME_PROPERTY = "Name";
        public const string NCRYPT_PROVIDER_HANDLE_PROPERTY = "Provider Handle";

        // BCrypt hash algorithm names สำหรับ CNG padding info
        public const string BCRYPT_SHA1_ALGORITHM = "SHA1";
        public const string BCRYPT_SHA256_ALGORITHM = "SHA256";
        public const string BCRYPT_SHA384_ALGORITHM = "SHA384";
        public const string BCRYPT_SHA512_ALGORITHM = "SHA512";

        #endregion

        #region โครงสร้าง (structs)

        // padding info สำหรับ PKCS#1 v1.5 เวลาเรียก NCryptSignHash
        [StructLayout(LayoutKind.Sequential)]
        public struct BCRYPT_PKCS1_PADDING_INFO
        {
            public IntPtr pszAlgId; // marshaled จาก string แบบ Unicode
        }

        // โครงสร้างเก็บ provider info ของ cert — ที่ Windows ผูกไว้ตอน enroll
        // ตรงกับ CRYPT_KEY_PROV_INFO ใน wincrypt.h
        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPT_KEY_PROV_INFO
        {
            public IntPtr pwszContainerName;   // Unicode string
            public IntPtr pwszProvName;        // Unicode string — ชื่อ provider จริงเช่น "eToken Base Cryptographic Provider"
            public uint dwProvType;            // PROV_RSA_FULL = 1 เป็นต้น
            public uint dwFlags;
            public uint cProvParam;
            public IntPtr rgProvParam;
            public uint dwKeySpec;             // AT_KEYEXCHANGE / AT_SIGNATURE / CERT_NCRYPT_KEY_SPEC
        }

        #endregion

        #region P/Invoke: crypt32.dll

        // ขอ handle private key ของ cert จาก Windows — ได้ทั้ง CSP หรือ CNG ขึ้นอยู่กับว่า cert ถูก enroll แบบไหน
        [DllImport("crypt32.dll", SetLastError = true)]
        public static extern bool CryptAcquireCertificatePrivateKey(
            IntPtr pCert,
            uint dwFlags,
            IntPtr pvParameters,
            out IntPtr phCryptProvOrNCryptKey,
            out uint pdwKeySpec,
            out bool pfCallerFreeProvOrNCryptKey);

        // อ่าน property ของ cert context — ใช้กับ CERT_KEY_PROV_INFO_PROP_ID เพื่อดึง provider info
        [DllImport("crypt32.dll", SetLastError = true)]
        public static extern bool CertGetCertificateContextProperty(
            IntPtr pCertContext,
            uint dwPropId,
            IntPtr pvData,
            ref uint pcbData);

        #endregion

        #region P/Invoke: advapi32.dll (CAPI / CSP)

        // สร้าง hash object สำหรับใส่ค่า hash ที่คำนวณแล้วจากภายนอก
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptCreateHash(
            IntPtr hProv,
            uint Algid,
            IntPtr hKey,
            uint dwFlags,
            out IntPtr phHash);

        // ใส่ค่า hash ที่คำนวณมาแล้วเข้า hash object (แทนการ hash ในตัว CSP)
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptSetHashParam(
            IntPtr hHash,
            uint dwParam,
            byte[] pbData,
            uint dwFlags);

        // เซ็น hash ด้วย private key ของ CSP
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptSignHash(
            IntPtr hHash,
            uint dwKeySpec,
            IntPtr sDescription,
            uint dwFlags,
            byte[] pbSignature,
            ref uint pdwSigLen);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptDestroyHash(IntPtr hHash);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptReleaseContext(IntPtr hProv, uint dwFlags);

        // อ่าน parameter ของ provider เช่น ชื่อ container / ชื่อ provider
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptGetProvParam(
            IntPtr hProv,
            uint dwParam,
            byte[] pbData,
            ref uint pdwDataLen,
            uint dwFlags);

        // ตั้งค่า PIN ของ Token แบบ CSP (แทนการใช้ CspParameters)
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool CryptSetProvParam(
            IntPtr hProv,
            uint dwParam,
            byte[] pbData,
            uint dwFlags);

        #endregion

        #region P/Invoke: ncrypt.dll (CNG)

        // เซ็น hash ด้วย CNG key handle
        [DllImport("ncrypt.dll")]
        public static extern int NCryptSignHash(
            IntPtr hKey,
            IntPtr pPaddingInfo,
            byte[] pbHashValue,
            int cbHashValue,
            byte[] pbSignature,
            int cbSignature,
            out int pcbResult,
            int dwFlags);

        [DllImport("ncrypt.dll")]
        public static extern int NCryptFreeObject(IntPtr hObject);

        // ตั้งค่า property ของ CNG key เช่น PIN
        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
        public static extern int NCryptSetProperty(
            IntPtr hObject,
            string pszProperty,
            byte[] pbInput,
            int cbInput,
            int dwFlags);

        // อ่าน property ของ CNG key เช่น ชื่อ provider
        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
        public static extern int NCryptGetProperty(
            IntPtr hObject,
            string pszProperty,
            byte[] pbOutput,
            int cbOutput,
            out int pcbResult,
            int dwFlags);

        #endregion

        #region helper methods

        // แปลงชื่อ hash algorithm (managed) เป็น CALG_* สำหรับ CSP
        public static uint MapHashAlgIdCapi(string hashAlgo)
        {
            switch ((hashAlgo ?? "").ToUpperInvariant())
            {
                case "SHA-1":
                case "SHA1":
                    return CALG_SHA1;
                case "SHA-256":
                case "SHA256":
                    return CALG_SHA_256;
                case "SHA-384":
                case "SHA384":
                    return CALG_SHA_384;
                case "SHA-512":
                case "SHA512":
                    return CALG_SHA_512;
                default:
                    throw new NotSupportedException("ไม่รองรับ hash algorithm: " + hashAlgo);
            }
        }

        // แปลงชื่อ hash algorithm เป็น BCrypt name สำหรับ CNG PKCS#1 padding info
        public static string MapHashAlgIdCng(string hashAlgo)
        {
            switch ((hashAlgo ?? "").ToUpperInvariant())
            {
                case "SHA-1":
                case "SHA1":
                    return BCRYPT_SHA1_ALGORITHM;
                case "SHA-256":
                case "SHA256":
                    return BCRYPT_SHA256_ALGORITHM;
                case "SHA-384":
                case "SHA384":
                    return BCRYPT_SHA384_ALGORITHM;
                case "SHA-512":
                case "SHA512":
                    return BCRYPT_SHA512_ALGORITHM;
                default:
                    throw new NotSupportedException("ไม่รองรับ hash algorithm: " + hashAlgo);
            }
        }

        // คำนวณ hash ของ message ด้วย managed API (ใช้ก่อนส่งไปเซ็น)
        public static byte[] ComputeHash(byte[] message, string hashAlgo)
        {
            using (HashAlgorithm h = CreateHashAlgorithm(hashAlgo))
            {
                return h.ComputeHash(message);
            }
        }

        private static HashAlgorithm CreateHashAlgorithm(string hashAlgo)
        {
            switch ((hashAlgo ?? "").ToUpperInvariant())
            {
                case "SHA-1":
                case "SHA1":
                    return SHA1.Create();
                case "SHA-256":
                case "SHA256":
                    return SHA256.Create();
                case "SHA-384":
                case "SHA384":
                    return SHA384.Create();
                case "SHA-512":
                case "SHA512":
                    return SHA512.Create();
                default:
                    throw new NotSupportedException("ไม่รองรับ hash algorithm: " + hashAlgo);
            }
        }

        // อ่าน provider info (ชื่อ provider + container + keySpec) จาก metadata ของ cert โดยตรง
        // ใช้ตัวนี้แทนการอ่านผ่าน handle เพราะ eToken CSP driver คืน container name ให้กับ PP_NAME แทน provider name
        // provInfo ที่ได้จาก CertGetCertificateContextProperty เป็นข้อมูลที่ Windows บันทึกไว้ตอน enroll — เชื่อถือได้
        public static bool GetCertKeyProvInfo(IntPtr pCertContext,
                                              out string provName,
                                              out string containerName,
                                              out uint keySpec)
        {
            provName = null;
            containerName = null;
            keySpec = 0;

            uint len = 0;
            if (!CertGetCertificateContextProperty(pCertContext, CERT_KEY_PROV_INFO_PROP_ID, IntPtr.Zero, ref len))
                return false;
            if (len == 0) return false;

            IntPtr buffer = Marshal.AllocHGlobal((int)len);
            try
            {
                if (!CertGetCertificateContextProperty(pCertContext, CERT_KEY_PROV_INFO_PROP_ID, buffer, ref len))
                    return false;

                CRYPT_KEY_PROV_INFO info = (CRYPT_KEY_PROV_INFO)Marshal.PtrToStructure(buffer, typeof(CRYPT_KEY_PROV_INFO));
                if (info.pwszProvName != IntPtr.Zero)
                    provName = Marshal.PtrToStringUni(info.pwszProvName);
                if (info.pwszContainerName != IntPtr.Zero)
                    containerName = Marshal.PtrToStringUni(info.pwszContainerName);
                keySpec = info.dwKeySpec;
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        // release handle ตาม type
        public static void ReleaseKeyHandle(IntPtr handle, bool isNCryptKey)
        {
            if (handle == IntPtr.Zero) return;
            if (isNCryptKey)
                NCryptFreeObject(handle);
            else
                CryptReleaseContext(handle, 0);
        }

        #endregion
    }
}
