using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;


namespace Client
{
    class Client
    {

        public static Socket hosterSocket;
        public static Socket master;

        public static string myIP;
        public static int myPort;
        public static FolderBrowserDialog hDirectory;
        public static FolderBrowserDialog rDirectory;
        public static string hostFolder;
        public static string receiveFolder;

        //public static Dictionary<string, FileData> clientFiles = new Dictionary<string,string>();

        [STAThread]
        static void Main(string[] args)
        {
            hDirectory = new FolderBrowserDialog();
            rDirectory = new FolderBrowserDialog();
            Console.WriteLine("Choose Host Directory");
            if (hDirectory.ShowDialog() == DialogResult.OK)
            {
                hostFolder = hDirectory.SelectedPath;
                Console.WriteLine("Host Directory: " + hostFolder.ToString());
            }
            Console.WriteLine("Choose Receive Directory");
            if (rDirectory.ShowDialog() == DialogResult.OK)
            {
                receiveFolder = rDirectory.SelectedPath;
                Console.WriteLine("Receive Directory: " + receiveFolder.ToString());
            }
            //hostFolder = "C:\\Users\\jpcen\\Desktop\\AAAAAAAA\\Host";
            //receiveFolder = "C:\\Users\\jpcen\\Desktop\\AAAAAAAA\\Receive";


            hosterSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            master = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


            //get filenames and filepaths
            string[] filePaths = Directory.GetFiles(hostFolder);
            string[] fileNames = JustFileNames(hostFolder);
            foreach (string fp in fileNames)
            {
                Console.WriteLine(fp);
            }

            //get filename path(necessary for sending information)


            //Server Info///////
            Console.Write("Enter server IP: ");
           // string connectIP = "192.168.1.103";
            string connectIP = "130.70.82.148";
            //string connectIP = Console.ReadLine();
            //string connectIP = "127.0.0.1";
            Console.WriteLine(connectIP);
            IPEndPoint serverIPEP = new IPEndPoint(IPAddress.Parse(connectIP), 30000);
            ////////////////////////////////////

            //Get Port and IP
            Random randPort = new Random();
            myPort = randPort.Next(30000, 30500);
            //int myPort = 30001;
            Console.WriteLine("Port: " + myPort);

            //For hosting and Serving Files to Other Clients
            myIP = GetLocalIPAddress();

            IPEndPoint myHostIP = new IPEndPoint(IPAddress.Parse(myIP), myPort);

            hosterSocket.Bind(myHostIP);

            Thread serveClient = new Thread(ListenThread);
            serveClient.Start();
        /////////////////////////////////////////////////////////

            Retry:
            try
            {
                master.Connect(serverIPEP);
                Console.WriteLine("Connecting from " + myIP);
            }
            catch
            {
                Console.WriteLine("ERROR! Could not connect to host");

                goto Retry;
            }

            //send stuff
            try
            {
                SocketSendString(master, string.Concat(myIP, ";", myPort.ToString()));
                //CSendFileInfo(myPort, myIPString, fileNames);
                //CSendFileInfo(myPort, myIPString, fileNames, filePaths);

                Console.WriteLine("Retreive FileInfo from Server = R ");
                Console.WriteLine("Update your FileInfo to Server = U");
                Console.WriteLine("Download file from server's Clients = D");
                Console.WriteLine("Print Serverside the Master Database = P");
                Console.WriteLine("Disconnect from Server, removes FileData = Q");

                string input = string.Empty;
                Thread.Sleep(1000);
                while (true)
                {

                    Console.WriteLine("Input: ");
                    input = Console.ReadLine();
                    switch (input)
                    {
                        case "r":
                        case "R":
                            SocketSendString(master, "RequestFileList");
                            CSReceiveFileInfo();
                            Console.WriteLine("Request: Successful Break");
                            break;
                        case "u":
                        case "U":
                            //hDirectory = new FolderBrowserDialog();
                            //rDirectory = new FolderBrowserDialog();
                            string[] newfilePaths = Directory.GetFiles(hostFolder);
                            string[] newfileNames = JustFileNames(hostFolder);
                            SocketSendString(master, "UpdateFileServer");
                            CSendFileInfo(myPort, myIP, newfileNames, newfilePaths);
                            Console.WriteLine("Update: Successful Break");
                            break;
                        case "d":
                        case "D":
                            try
                            {
                                SocketSendString(master, "DownloadFile");
                                string output = Data_Receive2(master);
                                Console.WriteLine(output);
                                string fileRequest = Console.ReadLine();

                                SocketSendString(master, fileRequest);
                                Console.WriteLine("Retriving File Owner");
                                string confirm = Data_Receive2(master);

                                if (confirm == "Correct")
                                {
                                    string hostdata = Data_Receive2(master);
                                    string[] HostInfo = ParseFileInfo(hostdata);
                                    string filePath = HostInfo[0];
                                    string HostIP = HostInfo[1];
                                    string HostPort = HostInfo[2];
                                    Console.WriteLine("HostInfo: \n" + filePath + "\n " + HostIP + "\n " + HostPort);
                                    Thread DownloadThread = new Thread(() => DownloadFileFromHost(filePath, HostIP, HostPort, fileRequest));
                                    DownloadThread.Start();
                                    while (DownloadThread.IsAlive)
                                    {
                                        
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Server Retrieved Incorrect Filename");
                                }
                                break;
                            }


                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                break;
                            }

                        case "p":
                        case "P":
                            SocketSendString(master, "PrintDataBase");
                            break;

                        case "q":
                        case "Q":
                            SocketSendString(master, "Disconnecting");
                            string RemoveConfirm = Data_Receive2(master);
                            Console.WriteLine(RemoveConfirm);
                            master.Shutdown(SocketShutdown.Both);
                            master.Close();
                            Thread.Sleep(3000);
                            Environment.Exit(0);
                            break;
                        default:
                            Console.WriteLine("Incorrect Input");
                            break;
                    }
                }
            }
            catch { }

        }

        //Listen and Serve File
        public static void ListenThread()
        {
            //Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket serverSocket;
            Console.WriteLine("Listening On: " + myIP + " \n on Port: " + myPort);
            while (true)
            {
                hosterSocket.Listen(0);

                serverSocket = hosterSocket.Accept();
                Console.WriteLine("HOSTER\\\\\\SUCCESSFUL ACCEPT");
                string successRec = Data_Receive2(serverSocket);
                Console.WriteLine("///////////////////////////////////////////////" + successRec);
                if (successRec == "RequestingFile")
                {
                    Console.WriteLine("HOSTER\\\\ Successful Request");
                    SocketSendString(serverSocket, "RequestAccepted");
                    string fileToSend = Data_Receive2(serverSocket);
                    byte[] fileData = File.ReadAllBytes(fileToSend);
                    //Console.WriteLine("SERVER//Attempting to send file" + fileToSend + " to Client Requester");
                    Console.WriteLine("HOSTER\\\\SENDING FILESIZE LEN: " + fileData.Length);
                    serverSocket.SendFile(fileToSend);
                    Console.WriteLine("Done Sending");

                    serverSocket.Shutdown(SocketShutdown.Both);
                    serverSocket.Close();
                    Console.WriteLine("Socket Closed");

                }
            }
        }

        public static void SocketSendString(Socket inSock, string input)
        {
            //Console.WriteLine("Sending: " + input);
            input = input + "<EOF>";
            inSock.Send(Encoding.ASCII.GetBytes(input));
        }

        public static string Data_Receive2(object cSocket)
        {
            string clientData = null;
            Socket clientSocket = (Socket)cSocket;
            byte[] Buffer;


            // while there's data to accept
            while (true)
            {
                Buffer = new Byte[516];
                int received = clientSocket.Receive(Buffer);
                // decode data sent
                clientData += Encoding.ASCII.GetString(Buffer, 0, received);
                if (clientData.IndexOf("<EOF>") > -1)
                {
                    //Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                    // clientData.Length, clientData);
                    break;
                }
            }
            return clientData.Split(new string[] { "<EOF>" }, StringSplitOptions.None)[0];
        }

        public static void CSendFileInfo(int myPort, string myIP, string[] fileNames, string[] filePaths)
        {
            int fileCount = fileNames.Length;

            SocketSendString(master, fileCount.ToString());
            string FCConfirm = Data_Receive2(master);
            //Console.WriteLine(FCConfirm);
            for (int i = 0; i < fileCount; i++)
            {
                SocketSendString(master, fileNames[i]);
                string FNConfirm = Data_Receive2(master);
                //Console.WriteLine("FNCheck: " + FNConfirm);
                SocketSendString(master, filePaths[i]);
                string FPConfirm = Data_Receive2(master);
                //Console.WriteLine("FPCheck: " + FPConfirm);
                SocketSendString(master, "CommenceInsert");
            }

        }

        public static void CSReceiveFileInfo()
        {
            string ReceiveConfrim = Data_Receive2(master);
            //Console.WriteLine(ReceiveConfrim);
            SocketSendString(master, "Ready");

            string fileList = Data_Receive2(master);
            string[] fileNames = fileList.Split(':').ToArray();
            foreach (string fn in fileNames)
            {
                Console.WriteLine(fn);
            }

        }

        public static void DownloadFileFromHost(string filePath, string hostIP, string hostPort, string fileName)
        {
            Socket ReceiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var myDownload = File.Create(Path.Combine(receiveFolder, fileName));

            Console.WriteLine("DOWNLAODER//SERVERS'S FILEPATH: " + filePath);
            Directory.SetCurrentDirectory(receiveFolder);
            Console.WriteLine("DOWNLOADER/////MY RECEIVE PATH: " + Directory.GetCurrentDirectory());

            ///////////////////////////////////////
            int truePort = int.Parse(hostPort);
            IPEndPoint clientHostIPEndPoint = new IPEndPoint(IPAddress.Parse(hostIP), truePort);
            ReceiverSocket.Connect(clientHostIPEndPoint);
            /////////////////////////////////////

            byte[] Buffer = new Byte[1024];
            int bytesRead;

            SocketSendString(ReceiverSocket, "RequestingFile");
            string acceptRetrieval = Data_Receive2(ReceiverSocket);
            if (acceptRetrieval == "RequestAccepted")
            {
                int totalBytesRead = 0;
                int readCount = 0;
                SocketSendString(ReceiverSocket, filePath);
                while ((bytesRead = ReceiverSocket.Receive(Buffer)) > 0)
                {
                    //Console.WriteLine(Buffer.ToArray().ToString());
                    totalBytesRead += bytesRead;
                    readCount += 1;
                    //Console.Write(totalBytesRead.ToString() + " ");
                    //Console.WriteLine(" " + bytesRead.ToString() + " " + readCount.ToString());
                    myDownload.Write(Buffer, 0, bytesRead);
                    //myDownload.WriteAsync(Buffer, 0, bytesRead);
                }
                myDownload.Close();

                Thread.CurrentThread.Abort();
                /*SocketSendString(ReceiverSocket, "DownloadCompleted");*/
            }
            //Console.WriteLine(acceptRetrieval);
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("IP was not found");
        }

        public static string[] JustFileNames(string hostDirectory)
        {
            //string[] fileNames = Directory.GetFiles(hostDirectory, "*.txt")
            //                         .Select(path => Path.GetFileName(path))
            //                         .ToArray();
            string[] filePaths, fileNames;
            var files = Directory.EnumerateFiles(hostDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(s => s.EndsWith(".txt") || s.EndsWith(".jpg") || s.EndsWith(".jpeg")).ToArray();

            filePaths = files;
            fileNames = new string[filePaths.Count()];
            

            for (int i = 0; i < filePaths.Count(); i++ )
            {
                fileNames[i] = Path.GetFileName(filePaths[i]);
                //fileNames[i].Split(splitPath, 0);
               // Console.WriteLine(i + ":  "+ fileNames[i]);

            }
                return fileNames;
        }

        public static string[] ParseFileInfo(string HostInfo)
        {
            string[] parsedInfo = HostInfo.Split(';');
            return parsedInfo;
        }

        //public static string SendIPandPort(string IP, int Port)
        //{

        //}

    }

}