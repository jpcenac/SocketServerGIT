using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;


namespace Client
{
    class Client
    {
        
        public static Socket listenerSocket;
        public static Socket master;
        //public static FolderBrowserDialog hDirectory;
        //public static FolderBrowserDialog rDirectory;
        public static string hostFolder;
        public static string receiveFolder;
        
        //public static Dictionary<string, FileData> clientFiles = new Dictionary<string,string>();

        
        static void Main(string[] args)
        {
            //hDirectory = new FolderBrowserDialog();
            //rDirectory = new FolderBrowserDialog();
            //Console.WriteLine("Choose Host Directory");
            //if(hDirectory.ShowDialog() == DialogResult.OK)
            //{
            //    hostFolder = hDirectory.SelectedPath;
            //    Console.WriteLine("Host Directory: " + hostFolder.ToString());
            //}
            //Console.WriteLine("Choose Receive Directory");
            //if(rDirectory.ShowDialog() == DialogResult.OK)
            //{
            //    receiveFolder = rDirectory.SelectedPath;
            //    Console.WriteLine("Receive Directory: " + receiveFolder.ToString());
            //}
            hostFolder = "C:\\Users\\jpc0759.GAMELAB\\Desktop\\AHostDir";
            receiveFolder = "C:\\Users\\jpc0759.GAMELAB\\Desktop\\AReceiveDir";


            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            master = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


            //get filenames and filepaths
            string[] filePaths = Directory.GetFiles(hostFolder);
            string[] fileNames = JustFileNames(hostFolder);
            foreach (string fp in fileNames)
            {
                Console.WriteLine(fp);
            }

            //get filename path(necessary for sending information)

            Console.Write("Enter server IP: ");
            //string connectIP = Console.ReadLine();
            string connectIP = "130.70.82.148";
            //string connectIP = "127.0.0.1";
            Console.WriteLine(connectIP);

            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(connectIP), 30000);

            Random randPort = new Random();
            int myPort = randPort.Next(30000, 30500);
            //int myPort = 30001;
            Console.WriteLine("Port: " + myPort);
            string myIPString = GetLocalIPAddress();
            IPEndPoint myPeerIP = new IPEndPoint(IPAddress.Parse(myIPString), myPort);

            listenerSocket.Bind(myPeerIP);

            Thread serveClient = new Thread(ListenThread);
            serveClient.Start();

            Retry:
            try
            {
                master.Connect(ip);
                Console.WriteLine("Connection Successful");
            }
            catch
            {
                Console.WriteLine("ERROR! Could not connect to host");
                
                goto Retry;
            }

            //send stuff
            try
            {
                //CSendFileInfo(myPort, myIPString, fileNames);
                //CSendFileInfo(myPort, myIPString, fileNames, filePaths);

                Console.WriteLine("Retreive FileInfo from Server = R ");
                Console.WriteLine("Update your FileInfo to Server = U");
                Console.WriteLine("Download file from server's Clients = D");
                Console.WriteLine("Print Serverside the Master Database = P");

                IPEndPoint myHostingIP = new IPEndPoint(IPAddress.Parse(myIPString), myPort);
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
                            SocketSendString(master, "UpdateFileServer");
                            CSendFileInfo(myPort, myIPString, fileNames, filePaths);
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
                                if(confirm == "Correct")
                                {
                                    string filePath = Data_Receive2(master);
                                    string HostIP = Data_Receive2(master);
                                    string HostPort = Data_Receive2(master);
                                    Console.WriteLine("HostInfo: \n" + filePath + "\n " + HostIP + "\n " + HostPort);
                                    Thread DownloadThread = new Thread(() => DownloadFileFromHost(filePath, HostIP, HostPort, fileRequest));
                                    DownloadThread.Start();
                                    while(DownloadThread.IsAlive)
                                    {

                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Server Retrieved Incorrect Filename");
                                }
                                break;
                            }
                        

                            catch(Exception e)
                            {
                                Console.WriteLine(e);
                                break;
                            }

                        case "p":
                        case "P":
                            SocketSendString(master, "PrintDataBase");
                            break;
                        default:
                            Console.WriteLine("Incorrect Input");
                            break;
                    }
                }
            }
            catch { }

        }

        //public static void DownloadStuff()
        //{
        //    var download = File.Create(Path.Combine(directory, filename.text));

        //    //Console.WriteLine("Starting to receive the file");

        //    // read the file in chunks of 1KB
        //    var buffer = new byte[1024];
        //    int bytesRead;
        //    while ((bytesRead = clientSocket.Receive(buffer)) > 0)
        //    {
        //        download.Write(buffer, 0, bytesRead);
        //    }

        //    download.Close();
        //}


        //Listen and Serve File
        public static void ListenThread()
        {
            //Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket serverSocket;
            Console.WriteLine("ListenThread Started");
            while (true)
            {
                listenerSocket.Listen(0);
                serverSocket = listenerSocket.Accept();
                string successRec = Data_Receive2(serverSocket);
                Console.WriteLine("///////////////////////////////////////////////" + successRec);
                if(successRec == "RequestingFile")
                {
                    Console.WriteLine("SERVER\\\\ Successful Request");
                    SocketSendString(serverSocket, "RequestAccepted");
                    string fileToSend = Data_Receive2(serverSocket);
                    byte[] fileData = File.ReadAllBytes(fileToSend);
                    //Console.WriteLine("SERVER//Attempting to send file" + fileToSend + " to Client Requester");
                    Console.WriteLine("SERVER\\\\SENDING FILESIZE LEN: " + fileData.Length);
                    serverSocket.SendFile(fileToSend);
                    Console.WriteLine("Done Sending");
                    //string fileConfirm = Data_Receive2(serverSocket);
                    //Console.WriteLine(fileConfirm);
                    //if(fileConfirm == "DownloadComplete")
                    //{
                    serverSocket.Shutdown(SocketShutdown.Both);
                    serverSocket.Close();
                    Console.WriteLine("Socket Closed");
                    //}
                    
                    
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
            Console.WriteLine("Sending FileInfo");
            SocketSendString(master, myIP);
            string ipConfirm = Data_Receive2(master);
            //Console.WriteLine(ipConfirm);
            SocketSendString(master, myPort.ToString());
            string portConfirm = Data_Receive2(master);
            //Console.WriteLine(portConfirm);
            SocketSendString(master, fileCount.ToString());
            string FCConfirm = Data_Receive2(master);
            //Console.WriteLine(FCConfirm);
            for(int i = 0; i < fileCount; i++)
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
            Console.WriteLine(ReceiveConfrim);
            SocketSendString(master, "Ready");
            string SendingConfirm = Data_Receive2(master);
            Console.WriteLine(SendingConfirm);
            string fileList = Data_Receive2(master);
            string[] fileNames = fileList.Split(':').ToArray();
            foreach(string fn in fileNames)
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
            IPEndPoint hostIPEndPoint = new IPEndPoint(IPAddress.Parse(hostIP), truePort);
            ReceiverSocket.Connect(hostIPEndPoint);
            /////////////////////////////////////

            byte[] Buffer = new Byte[1024];
            int bytesRead;

            SocketSendString(ReceiverSocket, "RequestingFile");
            string acceptRetrieval = Data_Receive2(ReceiverSocket);
            if(acceptRetrieval == "RequestAccepted")
            {
                int totalBytesRead = 0;
                int readCount = 0;
                SocketSendString(ReceiverSocket, filePath);     
                while((bytesRead = ReceiverSocket.Receive(Buffer)) > 0)
                {
                    //Console.WriteLine(Buffer.ToArray().ToString());
                    totalBytesRead += bytesRead;
                    readCount += 1;
                    Console.Write(totalBytesRead.ToString() + " ");
                    Console.WriteLine(" " + bytesRead.ToString() + " " + readCount.ToString());
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
            string[] fileNames = Directory.GetFiles(hostDirectory, "*.txt")
                                     .Select(path => Path.GetFileName(path))
                                     .ToArray();

            return fileNames;
        }

        


        //public static string GetFilePath(string fileName)
        //{

        //}

    }

}