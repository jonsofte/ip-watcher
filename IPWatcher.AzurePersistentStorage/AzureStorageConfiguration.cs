namespace IPWatcher.AzurePersistantStorage
{
    public class AzureStorageConfiguration
    {
        public _Authentication Authentication { get; set; } = new _Authentication();
        public _Blob Blob { get; set; } = new _Blob();

        public class _Authentication
        {
            public string X509CertificatePath { get; set; } = String.Empty;
            public string X509Password { get; set; } = String.Empty;
            public string AzureADTenantID { get; set; } = String.Empty;
            public string AzureADClientID { get; set; } = String.Empty;
        }
        public class _Blob
        {
            public string AccountUri { get; set; } = String.Empty;
            public string ContainerName { get; set; } = String.Empty;
            public string CurrentIPFile { get; set; } = String.Empty;
            public string ChangeLogFile { get; set; } = String.Empty;
        }
    }
}
