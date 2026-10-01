using System;


namespace DT.Model
{
    public class GuidGenerator
    {
        static readonly string Undefined_id = Guid.Empty.ToString("D");

        public static string UNDEFINED_ID
        {
            get { return GuidGenerator.Undefined_id; }
        }

        /// <summary>
        /// Generate a unique identifier based on RFC4122 (version 1 or 4 based on platform)
        /// </summary>
        /// <returns>An UUID as 32 digits separated by hyphens</returns>
        public static string FetchID()
        {
            // Internally calls CoCreateGuid
            // See https://docs.microsoft.com/en-us/windows/win32/api/combaseapi/nf-combaseapi-cocreateguid?redirectedfrom=MSDN
            return Guid.NewGuid().ToString("D");
        }

        public static string FetchID(string prefix)
        {
            return prefix + "-" + FetchID();
        }
    }
}
