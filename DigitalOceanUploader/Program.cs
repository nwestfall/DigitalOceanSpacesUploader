using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Security;

using HeyRed.Mime;
using Amazon.S3;
using Amazon.Runtime;

namespace DigitalOceanSpacesManager
{
    class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        public async static Task MainAsync(string[] args)
        {
            //Welcome user
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Digital Ocean Spaces Manager");
            Console.ResetColor();

            #region Keys
            Console.Write("Enter DO Access Key: ");
            ConsoleKeyInfo key;
            KeyManager.ACCESS_KEY.Clear();
            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    KeyManager.ACCESS_KEY.AppendChar(key.KeyChar);
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && KeyManager.ACCESS_KEY.Length > 0)
                    {
                        KeyManager.ACCESS_KEY.RemoveAt(KeyManager.ACCESS_KEY.Length - 1);
                        Console.Write("\b \b");
                    }
                }
            } while (key.Key != ConsoleKey.Enter);
            Console.Write("\n");
            Console.Write("Enter DO Secrey Key: ");
            KeyManager.SECRET_KEY.Clear();
            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    KeyManager.SECRET_KEY.AppendChar(key.KeyChar);
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && KeyManager.ACCESS_KEY.Length > 0)
                    {
                        KeyManager.ACCESS_KEY.RemoveAt(KeyManager.ACCESS_KEY.Length - 1);
                        Console.Write("\b \b");
                    }
                }
            } while (key.Key != ConsoleKey.Enter);
            Console.Write("\n");
            #endregion

            var client = new AmazonS3Client(KeyManager.SecureStringToString(KeyManager.ACCESS_KEY), KeyManager.SecureStringToString(KeyManager.SECRET_KEY), new AmazonS3Config()
            {
                ServiceURL = "https://nyc3.digitaloceanspaces.com"
            });
            client.ExceptionEvent += Client_ExceptionEvent;

            string filePath, uploadName, spaceName, contentType = string.Empty;
            Console.Write("Enter Space name to use: ");
            spaceName = Console.ReadLine();
            bool fileExists = false;
            do
            {
                Console.Write("Enter file location: ");
                filePath = Console.ReadLine();
                if (File.Exists(filePath))
                {
                    contentType = MimeGuesser.GuessFileType(filePath).MimeType;
                    fileExists = true;
                }
                else
                {
                    fileExists = false;
                    Console.WriteLine("File does not exist.  Please enter again.");
                }
            } while (!fileExists);
            Console.Write("Enter name to use when uploaded: ");
            uploadName = Console.ReadLine();
            Console.Write("Wipe away previous attempts? (Y/n): ");
            var wipeAway = Console.ReadLine();
            if(wipeAway == "Y")
            {
                var currentMultiParts = await client.ListMultipartUploadsAsync(spaceName);
                foreach (var multiPart in currentMultiParts.MultipartUploads)
                {
                    try
                    {
                        await client.AbortMultipartUploadAsync(currentMultiParts.BucketName, multiPart.Key, multiPart.UploadId);
                    }
                    catch(Exception) { }
                }

                Console.WriteLine("Wiped away previous upload attempts");
            }

            var fileInfo = new FileInfo(filePath);

            var multiPartStart = await client.InitiateMultipartUploadAsync(new Amazon.S3.Model.InitiateMultipartUploadRequest()
            {
                BucketName = spaceName,
                ContentType = contentType,
                Key = uploadName
            });
            try
            {
                var i = 0L;
                var n = 1;
                Dictionary<string, int> parts = new Dictionary<string, int>();
                while (i < fileInfo.Length)
                {
                    Console.WriteLine($"Starting upload for Part #{n}");
                    long partSize = 6000000;
                    var lastPart = (i + partSize) >= fileInfo.Length;
                    if (lastPart)
                        partSize = fileInfo.Length - i;
                    bool complete = false;
                    int retry = 0;
                    Amazon.S3.Model.UploadPartResponse partResp = null;
                    do
                    {
                        retry++;
                        try
                        {
                            partResp = await client.UploadPartAsync(new Amazon.S3.Model.UploadPartRequest()
                            {
                                BucketName = spaceName,
                                FilePath = filePath,
                                FilePosition = i,
                                IsLastPart = lastPart,
                                PartSize = partSize,
                                PartNumber = n,
                                UploadId = multiPartStart.UploadId,
                                Key = uploadName
                            });
                            complete = true;
                        }
                        catch(Exception)
                        {
                            Console.WriteLine($"Failed to upload part {n} on try #{retry}...");
                        }
                    } while (!complete && retry <= 3);

                    if (!complete || partResp == null)
                        throw new Exception($"Unable to upload part {n}... Failing");

                    parts.Add(partResp.ETag, n);
                    i += partSize;
                    n++;
                    Console.WriteLine($"Uploading {(((float)i/(float)fileInfo.Length) * 100).ToString("N2")} ({i}/{fileInfo.Length})");
                }

                Console.WriteLine("Done uploading!  Completing upload");
                var completePart = await client.CompleteMultipartUploadAsync(new Amazon.S3.Model.CompleteMultipartUploadRequest()
                {
                    UploadId = multiPartStart.UploadId,
                    BucketName = spaceName,
                    Key = uploadName,
                    PartETags = parts.Select(p => new Amazon.S3.Model.PartETag(p.Value, p.Key)).ToList()
                });
                Console.WriteLine("Successfully uploaded!");
            }
            catch(Exception ex)
            {
                var abortPart = await client.AbortMultipartUploadAsync(spaceName, uploadName, multiPartStart.UploadId);
                Console.WriteLine("Error while uploading! " + ex.Message);
                await Task.Delay(10000);
            }
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }

        private static void Client_ExceptionEvent(object sender, ExceptionEventArgs e)
        {
            Console.WriteLine($"Error with S3 Client occurred: {e}");
        }
    }
}
