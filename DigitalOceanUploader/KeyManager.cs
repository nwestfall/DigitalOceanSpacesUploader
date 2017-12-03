using System;
using System.Text;
using System.Security;
using System.IO;
using System.Runtime.InteropServices;

namespace DigitalOceanSpacesManager
{
    /// <summary>
    /// Manage Keys for Digital Ocean
    /// </summary>
    public static class KeyManager
    {
        /// <summary>
        /// Store Access Key
        /// </summary>
        public static readonly SecureString ACCESS_KEY = new SecureString();
        /// <summary>
        /// Store Secret Key
        /// </summary>
        public static readonly SecureString SECRET_KEY = new SecureString();

        /// <summary>
        /// Translate Secure String to String
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}
