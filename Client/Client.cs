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

namespace Client
{
    class Client
    {
        
        public static Socket listenerSocket;
        public static Socket master;
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
            if(hDirectory.ShowDialog() == DialogResult.OK)
            {
                hostFolder = hDirectory.SelectedPath;
                Console.WriteLine("Host Directory: " + hostFolder.ToString());
            }
            Console.WriteLine("Choose Receive Directory");
            if(rDirectory.ShowDialog() == DialogResult.OK)
            {
                receiveFolder = rDirectory.SelectedPath;
                Console.WriteLine("Receive Directory: " + receiveFolder.ToString());
            }

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
            Console.WriteLine(connectIP);

            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(connectIP), 30000);

            Random randPort = new Random();
            int myPort = randPort.Next(30000, 30500);
            //int myPort = 30001;
            string myIPString = GetLocalIPAddress();
            IPEndPoint myPeerIP = new IPEndPoint(IPAddress.Parse(myIPString), myPort);

            listenerSocket.Bind(myPeerIP);

            Thread serveClient = new Thread(ListenThread);
            serveClient.Start();

            Retry:
            try
            {
                Console.WriteLine("My Port is: " + myPort);
                master.Connect(ip);
                Console.WriteLine("Connect Successful");
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
                CSendFileInfo(myPort, myIPString, fileNames, filePaths);

                Console.WriteLine("Retreive FileInfo from Server = R ");
                Console.WriteLine("Update your FileInfo to Server = U");
                Console.WriteLine("Download file from server's Clients = D");

                IPEndPoint myHostingIP = new IPEndPoint(IPAddress.Parse(myIPString), myPort);
                string input = string.Empty;

                while (true)
                {
                    Console.WriteLine("Input: ");
                    input = Console.ReadLine();
                    switch (input)
                    {
                        case "r":
                        case "R":
                            SocketSendString(master, "RequestFileList");
                            int fileCount = int.Parse(Data_Receive2(master));
                            Console.WriteLine("Receiving " + fileCount.ToString() + " filenames from Master Server");
                            for (int i = 0; i < fileCount; i++)
                            {
                                string fileName = Data_Receive2(master);
                                Console.WriteLine(fileName);
                            }
                            break;
                        case "u":
                        case "U":
                            SocketSendString(master, "UpdateFileServer");
                            CSendFileInfo(myPort, myIPString, fileNames, filePaths);

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
                                string filePath = Data_Receive2(master);
                                string HostIP = Data_Receive2(master);
                                string HostPort = Data_Receive2(master);
                                Console.WriteLine("HostInfo: " + filePath + " " + HostIP + " " + HostPort);
                                Thread DownloadThread = new Thread(() => DownloadFileFromHost(filePath, HostIP, HostPort, fileRequest));
                                DownloadThread.Start();
                                break;
                            }
                            catch(Exception e)
                            {
                                Console.WriteLine(e);
                                break;
                            }
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

        public static void ListenThread()
        {
            Socket serverSocket;
            Console.WriteLine("ListenThread Started");
            while (true)
            {
                listenerSocket.Listen(0);
                serverSocket = listenerSocket.Accept();
                string successRec = Data_Receive2(serverSocket);
                if(successRec == "RequestingFile")
                {
                    Console.WriteLine("SERVER: Successful Request");
                    SocketSendString(serverSocket, "RequestAccepted");
                    string fileToSend = Data_Receive2(serverSocket);
                    byte[] fileData = File.ReadAllBytes(fileToSend);
                    Console.WriteLine("SERVER: Attempting to send file" + fileToSend + " to Client Requester");
                    serverSocket.SendFile(fileToSend);
                    Console.WriteLine("SERVER: Seding File");
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
                Buffer = new Byte[1024];
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
            Console.WriteLine("Client Preparing File Info \n Sending....");
            foreach(string fn in fileNames)
            {
                Console.WriteLine("Names: " + fn);
            }
            int index = 0;
            string portString = myPort.ToString();
            // Console.WriteLine("Attempting to send: " + ex);
            SocketSendString(master, portString);
            //SocketSendString(master, String.Empty);
            string PortRetConfirmation = Data_Receive2(master).ToString();
            Console.WriteLine(PortRetConfirmation);
            SocketSendString(master, myIP);
            string IPAddressConfirmation = Data_Receive2(master).ToString();
            Console.WriteLine(IPAddressConfirmation);
            SocketSendString(master, fileNames.Count().ToString());
            while (index < fileNames.Count())
            {
                Console.WriteLine("FileName " + fileNames[index] + " is being Sent");
                //Console.WriteLine(myFiles[0]);

                SocketSendString(master, fileNames[index]);
                Data_Receive2(master);
                SocketSendString(master, filePaths[index]);
                
                index = index + 1;
            }
        }

        public static void CSReceiveFileInfo()
        {
            int servFileCount = int.Parse(Data_Receive2(master));

            while (true)
            {
                for (int i = 0; i < servFileCount - 1; i++)
                {

                    string dynamicString = Data_Receive2(master);

                    Console.WriteLine("Here:" + dynamicString);
                }
            }
        }

        public static void DownloadFileFromHost(string filePath, string hostIP, string hostPort, string fileName)
        {
            
            Socket ReceiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine("DOWNLAODER: SERVERS'S FILEPATH: " + filePath);
            Directory.SetCurrentDirectory(receiveFolder);
            Console.WriteLine("DOWNLOADER: MY RECEIVE PATH: " + Directory.GetCurrentDirectory());

            int truePort = int.Parse(hostPort);
            byte[] Buffer = new Byte[1024];
            int bytesRead;

            var myDownload = File.Create(Path.Combine(receiveFolder, fileName));

            IPEndPoint hostIPEndPoint = new IPEndPoint(IPAddress.Parse(hostIP), truePort);
            ReceiverSocket.Connect(hostIPEndPoint);
            SocketSendString(ReceiverSocket, "RequestingFile");
            string acceptRetrieval = Data_Receive2(ReceiverSocket);
            if(acceptRetrieval == "RequestAccepted")
            {
                Console.WriteLine("DOWNLOADER: REQUESTING FILEPATH: " + filePath);
                
                SocketSendString(ReceiverSocket, filePath);
                Console.WriteLine("DOWNLOADER: BEGINNING RECEIVE");
                while((bytesRead = ReceiverSocket.Receive(Buffer)) > 0)
                {
                    myDownload.Write(Buffer, 0, bytesRead);
                }
               
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