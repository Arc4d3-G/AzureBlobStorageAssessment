using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AzureBlobStorageAssessment
{
    internal class Program
    {
        #region Global Variables
        // Retrieve the connection string (saved as env.variable) for use with the application. 
        static readonly string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        // Default container name & upload/download paths
        static string containerName = "main-container";
        static readonly string localPath = @"C:\data";
        static readonly string localDownloadPath = @"C:\data\downloads";
        #endregion

        static void Main(string[] args)
        {
            #region initialization
            Console.WriteLine($"Connecting to storage account...");

            // Create a BlobServiceClient object with the connection string
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            Console.WriteLine($"Connection established to {blobServiceClient.AccountName}!");

            // Get container by the default name, else create a new one
            Console.WriteLine($"Getting default container {containerName}...");
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            bool containerFound = containerClient.Exists();
            if (containerFound)
            {
                Console.WriteLine($"Container {containerName} was found.");
            }
            else
            {
                Console.WriteLine($"Container {containerName} was not found. Creating new container...");
                blobServiceClient.CreateBlobContainerAsync(containerName);
            }
            #endregion
            // Ascii art header for fun :)
            Console.WriteLine(
                @"
  ____  _       _       ____  _                 
 | __ )| | ___ | |__   / ___|| |_ ___  _ __ ___ 
 |  _ \| |/ _ \| '_ \  \___ \| __/ _ \| '__/ _ \
 | |_) | | (_) | |_) |  ___) | || (_) | | |  __/
 |____/|_|\___/|_.__/  |____/ \__\___/|_|  \___|
 |_ _|_ __ | |_ ___ _ __ / _| __ _  ___ ___     
  | || '_ \| __/ _ \ '__| |_ / _` |/ __/ _ \    
  | || | | | ||  __/ |  |  _| (_| | (_|  __/    
 |___|_| |_|\__\___|_|  |_|  \__,_|\___\___|    by Dewald breed
"
);

            // loop program unti exit condition is met
            bool exit = false;
            int inputValue;
            do
            {
                // Display menu at the start of each loop itteration
                ShowMenu();

                #region input validation
                while (true)
                {
                    Console.Write("Input: ");
                    string input = Console.ReadLine();
                    try
                    {
                        inputValue = int.Parse(input);
                        if (inputValue > 5 || inputValue < 0)
                        {
                            throw new Exception();
                        } else if (inputValue == 0)
                        {
                            exit = true;
                            break;
                        } else
                        {
                            break;
                        }
                    } catch (Exception)
                    {
                        Console.WriteLine("Invalid input. Please input a valid number (0-5).");
                    }
                }
                #endregion

                #region Menu switch case
                switch (inputValue)
                {
                    case 1:
                        #region Upload
                        // Check if directory for uploads exists, else create it.
                        Console.WriteLine("Before proceeding, please ensure the file to be uploaded is located in the \"C:\\data\" folder.");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        if (Directory.Exists(localPath))
                        {
                            Console.WriteLine("Directory \"C:\\data\" found!");
                        }
                        else
                        {
                            Console.WriteLine("Directory \"C:\\data\" could not be found. Creating new directory...");
                            try
                            {
                                Directory.CreateDirectory(localPath);
                                Console.WriteLine("Directory \"C:\\data\" successfully created!");
                            } catch (Exception e)
                            {
                                Console.WriteLine($"Failed to create directory. Error Message: {e.Message}\n" +
                                    $"Exiting application...");
                                exit = true;
                            }
                            break;
                        }

                        // Check if any files exists in data directory for upload, else cancel the upload process
                        if (Directory.GetFiles(localPath).Length == 0)
                        {
                            Console.WriteLine($"No files found. Please place files to upload in {localPath} and try again.");
                            break;
                        }

                        // Get a list of files to upload from PromptForUpload and pass it to Upload Method
                        List<string> filesToUpload = PromptForUpload();
                        Upload(filesToUpload, containerClient);
                        break;

                    #endregion
                    case 2:
                        #region View
                        // Check if container cotnains any blobs to display, else cancel Viewing process.
                        if (containerClient.GetBlobs().Count() == 0)
                        {
                            Console.WriteLine($"\nContainer \"{containerName}\" in empty. Upload files to view them.\n");
                            break;
                        }
                        // Call ListBlobs method which displays all blobs in active container
                        ListBlobs(containerClient);
                        break;

                    #endregion
                    case 3:
                        #region Download
                        // Check if download directory exists, else create it
                        if (Directory.Exists(localDownloadPath))
                        {
                            Console.WriteLine($"\nDirectory \"{localDownloadPath}\" found!");
                        }
                        else
                        {
                            Console.WriteLine($"Directory \"{localDownloadPath}\" could not be found. Creating new directory...");
                            try
                            {
                                Directory.CreateDirectory(localDownloadPath);
                                Console.WriteLine($"Directory \"{localDownloadPath}\" successfully created!");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Failed to create directory. Error Message: {e.Message}\n" +
                                    $"Exiting application...");
                                exit = true;
                            }
                        }

                        // Check if container has any blobs to download, else cancel download process
                        if (containerClient.GetBlobs().Count() == 0)
                        {
                            Console.WriteLine($"Container \"{containerName}\" has nothing to download. Aborting download.");
                            break;
                        }
                        // Get list of blobs from PromptForOperation and pass it to Download method
                        List<BlobItem> filesToDownload = PromptForOperation(containerClient, "Download");
                        Download(filesToDownload, containerClient);
                        break;
                        #endregion
                    case 4:
                        #region Delete
                        // Check if container has any files to delete, else cancel delete process
                        if (containerClient.GetBlobs().Count() == 0)
                        {
                            Console.WriteLine($"Container \"{containerName}\" has nothing to delete.");
                            break;
                        }
                        // Get list of blobs from PromptForOperation and pass it to Delete Method
                        List<BlobItem> filesToDelete = PromptForOperation(containerClient, "Delete");
                        Delete(filesToDelete, containerClient);
                        break;

                    #endregion
                    case 5:
                        #region Change Container
                        // change active container to the one returned by the Prompt method
                        containerName = PromptForContainer(blobServiceClient);
                        containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                        break;

                        #endregion
                }
                #endregion
            } while (!exit);
        }
        // Method prints a series of lines listing menu options, as well as the currect active container
        static void ShowMenu()
        {
            Console.WriteLine(
                $"\nActive Container: {containerName}\nWhat would you like to do?\n" +
                "-----------------------------------------------\n" +
                "Input 1 to Upload a Blob to Storage\n" +
                "Input 2 to View Blobs in Storage\n" +
                "Input 3 to Download a Blob from Storage\n" +
                "Input 4 to Delete a Blob from Storage\n" +
                "Input 5 to Change Active Container\n" +
                "Input 0 to Exit\n" +
                "-----------------------------------------------\n");
        }

        // Method gets all blobs from the active container and lists them
        static void ListBlobs(BlobContainerClient containerClient)
        {
            Console.WriteLine($"\nViewing files in container: {containerName}\n-----------------------------------------------");

            List<BlobItem> blobItems = containerClient.GetBlobs().ToList();
            for (int i = 0; i < blobItems.Count; i++)
            {
                Console.WriteLine($"File #{i + 1} - {blobItems.ElementAt(i).Name}");
            }
            Console.WriteLine("-----------------------------------------------");
        }
        // Method that prompts the user to select a file to upload (or select all files) and
        // returns a list of file names.
        public static List<string> PromptForUpload()
        {
            // list to be populated and returned
            List<string> filesToUpload = new List<string>();

            Console.WriteLine("\nWhich file would you like to upload?\n-----------------------------------------------");
            string[] foundFiles = Directory.GetFiles(localPath);

            for (int i = 0; i < foundFiles.Length; i++)
            {
                string file = foundFiles[i];
                Console.WriteLine($"File #{i + 1} - {Path.GetFileName(file)}");
            }
            Console.WriteLine("-----------------------------------------------");

            Console.WriteLine("\nInput a File Number to upload the specified file, or * to upload all files.");

            // Input validation
            int chosenFile;
            while (true)
            {
                Console.Write("Input: ");
                string input = Console.ReadLine();

                try
                {
                    if (input == "*")
                    {
                        foreach (string file in foundFiles)
                        {
                            filesToUpload.Add(file);
                        }
                        break;
                    }

                    chosenFile = int.Parse(input);

                    if (chosenFile > foundFiles.Length + 1 || chosenFile <= 0)
                    {
                        throw new Exception($"File with number {input} not found.");
                    }
                    else
                    {
                        filesToUpload.Add(foundFiles[chosenFile - 1]); 
                        break;
                    }
                    
                } catch (Exception e) 
                {
                    Console.WriteLine($"Invalid input: {e.Message}. " +
                        $"\nPlease provide a digit representing a file number, or a \"*\"" +
                    "character to select All Files");

                }


            }
            return filesToUpload;
        }

        // Similar to PromptForUpload, but this method returns a list of blobs that can either be used
        // for downloading or deleting blobs. 
        public static List<BlobItem> PromptForOperation(BlobContainerClient containerClient, string operation)
        {
            // the returned list
            List<BlobItem> filesToOperate = new List<BlobItem>();

            // list of all blobs in container
            List<BlobItem> blobItems = containerClient.GetBlobs().ToList();

            Console.WriteLine($"\nWhich file would you like to {operation}?\n-----------------------------------------------");

            for (int i = 0; i < blobItems.Count; i++)
            {
                Console.WriteLine($"File #{i + 1} - {blobItems.ElementAt(i).Name}");
            }
            Console.WriteLine("-----------------------------------------------");

            Console.WriteLine($"\nInput a File Number to {operation} the specified file, or * to {operation} all files.");

            // Input validation
            int chosenFile;
            while (true)
            {
                Console.Write("Input: ");
                string input = Console.ReadLine();

                try
                {

                    if (input == "*")
                    {
                        foreach (BlobItem blobItem in blobItems)
                        {
                            filesToOperate.Add(blobItem);
                        }
                        break;

                    }

                    chosenFile = int.Parse(input);

                    if (chosenFile > blobItems.Count() || chosenFile <= 0)
                    {
                        throw new Exception($"File with number {input} not found.");
                    }
                    else
                    {
                        filesToOperate.Add(blobItems[chosenFile - 1]);
                        break;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Invalid input: {e.Message}. " +
                        $"\nPlease provide a digit representing a file number, or a \"*\"" +
                    "character to select All Files");

                }

            }
            return filesToOperate;
        }

        // Method takes a list of file names and attempts to upload them to the active container
        public static void Upload(List<string> files, BlobContainerClient containerClient)
        {
            Console.WriteLine("Uploading files...\n");
            int jobNumber = 1;
            foreach (string file in files)
            {
                try
                {
                    string fileName = Path.GetFileName(file);
                    BlobClient blobClient = containerClient.GetBlobClient(fileName);
                    Console.WriteLine($"Uploading file {fileName} to Blob storage (Job {jobNumber} of {files.Count})...");
                    blobClient.UploadAsync(file, true);
                    jobNumber++;
                } catch (Exception e) 
                { 
                    Console.WriteLine(e); 
                }
            }
            Console.WriteLine("\nUpload complete!\n");
        }

        // Method takes a list of blobs from the active container and attempts to download them
        // to the local download path. A Guid is prepended to each file to avoid overwrites
        // and differentiate downloaded files from local ones.
        public static void Download(List<BlobItem> blobItems, BlobContainerClient containerClient)
        {
            Console.WriteLine("Downloading files...\n");
            int jobNumber = 1;
            foreach (BlobItem blobItem in blobItems)
            {
                try
                {
                    Console.WriteLine($"Downloading file {blobItem.Name} from Blob storage (Job {jobNumber} of {blobItems.Count})...");
                    BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);
                    string downloadPath = $"{localPath}\\downloads\\{Guid.NewGuid().ToString()}-{blobItem.Name}";
                    blobClient.DownloadTo(downloadPath);
                    jobNumber++;
                }
                catch (Exception e) { Console.WriteLine(e); }

            }
            Console.WriteLine("\nDownload complete!\n");
        }

        // Method takes a list of blobs from the active container and attempts to delete them
        public static void Delete(List<BlobItem> blobItems, BlobContainerClient containerClient) 
        {
            Console.WriteLine("Deleting files...\n");
            int jobNumber = 1;
            foreach (BlobItem blobItem in blobItems)
            {
                try
                {
                    Console.WriteLine($"Deleting file {blobItem.Name} from Blob storage (Job {jobNumber} of {blobItems.Count})...");
                    BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);
                    blobClient.Delete();
                    jobNumber++;
                }
                catch (Exception e) { Console.WriteLine(e); }

            }
            Console.WriteLine("\nOperation complete!\n");
        }

        // Method prompts the user to select an existing container to set as active, or to
        // create a new one. Opting to create a new one calls the CreateNewContainer method.
        // Returns the selected container's name as a string.
        public static string PromptForContainer(BlobServiceClient blobServiceClient)
        {
            string selectedContainer;
            Console.WriteLine("\nAvailible containers\n-----------------------------------------------");
            int blobNum = 1;
            foreach (BlobContainerItem container in blobServiceClient.GetBlobContainers())
            {
                Console.WriteLine($"#{blobNum} - {container.Name}");
                blobNum++;
            }
            Console.WriteLine("-----------------------------------------------");

            Console.WriteLine("\nInput a Container Number to set the Active Container, or * to create new Container.");

            // Input validation
            int chosenContainerNum;
            while (true)
            {
                Console.Write("Input: ");
                string input = Console.ReadLine();

                try
                {
                    if (input == "*")
                    {
                        selectedContainer = CreateNewContainer(blobServiceClient);
                        break;
                    }

                    chosenContainerNum = int.Parse(input);

                    if (chosenContainerNum > blobServiceClient.GetBlobContainers().Count() || chosenContainerNum <= 0)
                    {
                        throw new Exception($"Container with number {input} not found.");
                    }
                    else
                    {
                        selectedContainer = blobServiceClient.GetBlobContainers().ElementAt(chosenContainerNum - 1).Name;
                        break;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Invalid input: {e.Message}. " +
                        $"\nPlease provide a digit representing a Container number, or a \"*\" " +
                    "character to create a new Container");

                }
            }
            return selectedContainer;
        }

        // Method prompts the user for a new container name and validates it using regex.
        // If valid, the container is created and the method returns the new container's name.
        public static string CreateNewContainer(BlobServiceClient blobServiceClient)
        {
            string newContainerName;
            Console.WriteLine("Please input the new Container name. Container names must adhere to the following:\n\n" +
                "\t 3 to 63 Characters\n" +
                "\t Starts With Letter or Number\n" +
                "\t Contains Letters, Numbers, and Dash (-)\n" +
                "\t All letters must be lowercase\n" +
                "\t Every Dash (-) Must Be Immediately Proceded and Followed by a Letter or Number\n");
            
            // Input validation
            while (true)
            {
                Console.Write("Container Name: ");
                string input = Console.ReadLine();
                try
                {
                    if (!Regex.IsMatch(input, @"^[a-z0-9](([a-z0-9\-[^\-])){1,61}[a-z0-9]$"))
                    {
                        throw new Exception("Invalid container name");
                    } else
                    {
                        blobServiceClient.CreateBlobContainerAsync(input);
                        newContainerName = input;
                        Console.WriteLine($"New container \"{input}\" was successfully created and set to active...");
                        break;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Invalid container name. Please ensure the provided name follows all naming rules.");
                }
            }

            return newContainerName;

        }
    }
}
