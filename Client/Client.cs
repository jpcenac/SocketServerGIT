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
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Choose Host Directory");
            if (hDirectory.ShowDialog() == DialogResult.OK)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                hostFolder = hDirectory.SelectedPath;
                Console.WriteLine("Host Directory: " + hostFolder.ToString());
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Choose Receive Directory");
            if (rDirectory.ShowDialog() == DialogResult.OK)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                receiveFolder = rDirectory.SelectedPath;
                Console.WriteLine("Receive Directory: " + receiveFolder.ToString());
            }

            //Socket for Hosting Files between Peers
            hosterSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //Socket for Connection Manager/Server
            master = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


            //get filenames and filepaths
            string[] filePaths = Directory.GetFiles(hostFolder);
            string[] fileNames = JustFileNames(hostFolder);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Current Files in Directory");
            foreach (string fp in fileNames)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(":::::" + fp);
            }

            //get filename path(necessary for sending information)

            //Generates Random port for client
            Random randPort = new Random();
            myPort = randPort.Next(30001, 35000);
            //int myPort = 30001;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("My Port::::::::::::::: ");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(myPort);


            //Server Info///////
            string connectIP = String.Empty;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Enter IP = 0 ////// Use Testing IP(130.82.148) = 9");
            while(connectIP == String.Empty)
            {
                string IPInput = Console.ReadLine();
                if (IPInput == "0")
                {
                    Console.Write("Enter server IP: ");
                    connectIP = Console.ReadLine();
                }
                else if (IPInput == "9")
                {
                    connectIP = "130.70.82.148";
                }
                else
                {
                    Console.WriteLine("Incorrect Input");
                    
                }
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(connectIP);
            IPEndPoint serverIPEP = new IPEndPoint(IPAddress.Parse(connectIP), 30000);
            ////////////////////////////////////

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
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Connecting from ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(myIP);
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("ERROR! Could not connect to host");

                goto Retry;
            }

            //send stuff
            try
            {
                SocketSendString(master, string.Concat(myIP, ";", myPort.ToString()));
                //CSendFileInfo(myPort, myIPString, fileNames);
                //CSendFileInfo(myPort, myIPString, fileNames, filePaths);
                Console.ForegroundColor = ConsoleColor.DarkYellow;

                Console.WriteLine("Retreive FileInfo from Server = R ");
                Console.WriteLine("Update your FileInfo to Server = U");
                Console.WriteLine("Download file from server's Clients = D");
                Console.WriteLine("Print Serverside the Master Database = P");
                Console.WriteLine("Disconnect from Server, removes FileData = Q");

                string input = string.Empty;
                Thread.Sleep(500);
                while (true)
                {
                    Console.ResetColor();
                    Console.WriteLine("Input: ");
                    input = Console.ReadLine();
                    switch (input)
                    {
                        case "r":
                        case "R":
                            SocketSendString(master, "RequestFileList");
                            CSReceiveFileInfo();
                            Console.ForegroundColor = ConsoleColor.DarkBlue;
                            Console.BackgroundColor = ConsoleColor.Green;

                            Console.WriteLine("Request Successful////RETURNING");
                            break;
                        case "u":
                        case "U":
                            //hDirectory = new FolderBrowserDialog();
                            //rDirectory = new FolderBrowserDialog();
                            string[] newfilePaths = Directory.GetFiles(hostFolder);
                            string[] newfileNames = JustFileNames(hostFolder);
                            SocketSendString(master, "UpdateFileServer");

                            CSendFileInfo(myPort, myIP, newfileNames, newfilePaths);

                            Console.ForegroundColor = ConsoleColor.DarkBlue;
                            Console.BackgroundColor = ConsoleColor.Green;
                            Console.WriteLine("Update Successful////RETURNING");
                            break;
                        case "d":
                        case "D":
                            try
                            {
                                SocketSendString(master, "DownloadFile");
                                string output = Data_Receive2(master);
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.WriteLine(output);
                                string fileRequest = Console.ReadLine();

                                SocketSendString(master, fileRequest);
                                Console.ForegroundColor = ConsoleColor.Yellow;
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
                                else if(confirm == "Correct+")
                                {
                                    Console.WriteLine("Server Returning with Hosts of File");
                                    SocketSendString(master, "ConfirmGo");
                                    string hostListCountSt = Data_Receive2(master);
                                    Console.WriteLine(hostListCountSt);


                                    int hostListCount = int.Parse(hostListCountSt);
                                    string[] indexKVP = new string[hostListCount];
                                    Dictionary<int, string[]> indexDict = new Dictionary<int, string[]>();


                                    SocketSendString(master, "RetrieveHosts");
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.Write("MYIP:::::::::: ");
                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                    Console.WriteLine(myIP);


                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.Write("MYPORT:::::::: ");
                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                    Console.WriteLine(myPort.ToString());


                                    for(int i = 0; i < hostListCount; i++)
                                    {
                                        indexKVP[i] = Data_Receive2(master);
                                        string[] valueHostInfo = new string[3];
                                        string entryKVP = indexKVP[i];
                                        string[] decodeKVP = entryKVP.Split(';');
                                        int keyHostInfo = int.Parse(decodeKVP[0]);
                                        indexDict.Add(keyHostInfo, valueHostInfo);
                                        indexDict[keyHostInfo][0] = decodeKVP[1];
                                        indexDict[keyHostInfo][1] = decodeKVP[2];
                                        indexDict[keyHostInfo][2] = decodeKVP[3];
                                        if(myIP != indexDict[keyHostInfo][0] 
                                            && myPort.ToString() != indexDict[keyHostInfo][2])
                                        {
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.Write("FILE INDEX:::::::: ");
                                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                                            Console.WriteLine(decodeKVP[0]);

                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.Write(" HostIP::::::::: ");
                                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                                            Console.WriteLine(decodeKVP[2]);

                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.Write(" HostPORT::::::: ");
                                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                                            Console.WriteLine(decodeKVP[3] + "\n");
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.Write("FILE INDEX:::::::: ");
                                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                                            Console.WriteLine(decodeKVP[0]);

                                            Console.ForegroundColor = ConsoleColor.DarkGray;
                                            Console.Write(" myIP::::::::::: ");
                                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                                            Console.WriteLine(decodeKVP[2]);

                                            Console.ForegroundColor = ConsoleColor.DarkGray;
                                            Console.Write(" myPORT::::::::: ");
                                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                                            Console.WriteLine(decodeKVP[3] + "\n");
                                        }


                                    }
                                    Console.WriteLine(Data_Receive2(master));
                                    string IndexChoice = Console.ReadLine();

                                    int intIndex = int.Parse(IndexChoice);
                                    SocketSendString(master, intIndex.ToString());
                                    if(intIndex  <= hostListCount)
                                    {
                                        string hostFilePath = indexDict[intIndex][0];
                                        string hostIP = indexDict[intIndex][1];
                                        string hostPort = indexDict[intIndex][2].ToString();
                                        Console.WriteLine("HostInfo: \n" + hostFilePath + "\n " + hostIP + "\n " + hostPort);
                                        Thread DownloadThread = new Thread(() => DownloadFileFromHost(hostFilePath, hostIP, hostPort, fileRequest));
                                        DownloadThread.Start();
                                        while (DownloadThread.IsAlive)
                                        {

                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Incorrect Input");
                                        break;
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
                            Console.WriteLine("Disconnecting");
                            SocketSendString(master, "Disconnecting");
                            string RemoveConfirm = Data_Receive2(master);
                            Console.WriteLine(RemoveConfirm);
                            master.Shutdown(SocketShutdown.Both);
                            master.Close();
                            Thread.Sleep(1000);
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

            while (true)
            {
                hosterSocket.Listen(0);

                serverSocket = hosterSocket.Accept();
                Console.ForegroundColor = ConsoleColor.DarkGreen;
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
                    Console.ResetColor();
                    Console.WriteLine("Input: ");

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
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Server has available::");
            foreach (string fn in fileNames)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(fn);
            }

        }

        public static void DownloadFileFromHost(string filePath, string hostIP, string hostPort, string fileName)
        {
            Socket ReceiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var myDownload = File.Create(Path.Combine(receiveFolder, fileName));
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("DOWNLAODER//HOSTER'S FILEPATH::: ");
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine(filePath);
            Directory.SetCurrentDirectory(receiveFolder);

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("DOWNLOADER/////MY RECEIVE PATH:: ");
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine(Directory.GetCurrentDirectory());

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
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("RECEIVING THREAD CLOSING");
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