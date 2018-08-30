using System;
using System.Text;
using System.Security;
using System.IO;
using System.Runtime.InteropServices;

namespace DigitalOceanUploader.Shared
{
    /// <summary>
    /// Manage Keys for Digital Ocean
    /// </summary>
	public class KeyManager : IDisposable
    {
        /// <summary>
        /// Store Access Key
        /// </summary>
        public readonly SecureString AccessKey = new SecureString();
        /// <summary>
        /// Store Secret Key
        /// </summary>
        public readonly SecureString SecretKey = new SecureString();

		/// <summary>
		/// Initializes a new instance of the <see cref="T:DigitalOceanUploader.Shared.KeyManager"/> class.
		/// </summary>
		public KeyManager() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:DigitalOceanUploader.Shared.KeyManager"/> class.
		/// </summary>
		/// <param name="accessKey">Access key.</param>
		/// <param name="secretKey">Secret key.</param>
		public KeyManager(string accessKey, string secretKey)
		{
			foreach (var c in accessKey)
				AccessKey.AppendChar(c);
			foreach (var c in secretKey)
				SecretKey.AppendChar(c);
		}

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

		public void Dispose()
		{
			AccessKey?.Dispose();
			SecretKey?.Dispose();
		}
    }
}
