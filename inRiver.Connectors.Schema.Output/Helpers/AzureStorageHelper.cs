using System;
using System.IO;
using Microsoft.WindowsAzure.Storage; 
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;


namespace inRiver.Connectors.Schema.Output.Helpers
{
    /// <summary>
    /// Encapsulates interaction with Azure Storage
    /// </summary>
    internal class AzureStorageHelper
    {

        internal AzureStorageHelper(AzureStorageMedium storageMedium, AzureEnvironmentInfo aei)
        {
            this.storageMedium = storageMedium;
            this.azureEnvirionmentInfo = aei;
        }

        internal AzureStorageMedium storageMedium { get; set; }

        internal AzureEnvironmentInfo azureEnvirionmentInfo { get; set; }

        internal string uploadFile(byte[] file, string path)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(azureEnvirionmentInfo.AzureStorageConnection); 
            string url = String.Format("{0}/{1}/", azureEnvirionmentInfo.AzureEndpointUrl, azureEnvirionmentInfo.AzureContainer);
            switch (storageMedium)
            {
                case AzureStorageMedium.Blob:
                    saveAzureBlob(file, path, storageAccount);
                    break;
                case AzureStorageMedium.File:
                    saveAzureFile(file, path, storageAccount);
                    break;
                default:
                    throw new ApplicationException("Unkown Azure Storage Medium");
            }
            url += path;
            return url;
        }

        void saveAzureBlob(byte[] file, string fileName, CloudStorageAccount storageAccount)
        {
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(azureEnvirionmentInfo.AzureContainer);
            // Create the container if it doesn't already exist.
            container.CreateIfNotExistsAsync().Wait();
            // Set Public Read Access
            container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob fileBlob = container.GetBlockBlobReference(fileName);
            if (fileBlob.Exists())
            {
                fileBlob.FetchAttributes();
            }
            if (fileBlob.Properties.Length != file.Length)
            {
                fileBlob.UploadFromByteArrayAsync(file, 0, file.Length, null, null, null).Wait();
            }
        }

        void saveAzureFile(byte[] file, string filePath, CloudStorageAccount storageAccount)
        {

            string[] pathParts = filePath.Split('/');

            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();

            CloudFileShare share = fileClient.GetShareReference(azureEnvirionmentInfo.AzureContainer);

            share.CreateIfNotExistsAsync().Wait();




            CloudFileDirectory cloudFileDirectory = share.GetRootDirectoryReference();
            for(int i=0; i < pathParts.Length - 1; i++)
            {
                CloudFileDirectory nextFolder = cloudFileDirectory.GetDirectoryReference(pathParts[i]);
                nextFolder.CreateIfNotExistsAsync().Wait();
                cloudFileDirectory = nextFolder;
            }

            //Create a reference to the filename that you will be uploading
            CloudFile cloudFile = cloudFileDirectory.GetFileReference(pathParts[pathParts.Length - 1]);

            //Upload the file to Azure.
            cloudFile.UploadFromByteArrayAsync(file, 0, file.Length, null, null, null).Wait();
        }

    }
}
