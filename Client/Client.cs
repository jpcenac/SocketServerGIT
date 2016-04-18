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
        public static string id;
        public static List<string> myFiles = new List<string>();


        //public static Dictionary<string, FileData> clientFiles = new Dictionary<string,string>();

        static void Main(string[] args)
        {
            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            id = Guid.NewGuid().ToString();


            //get filenames
            string[] fileNames = Directory.GetFiles("C:\\Users\\jpc0759.GAMELAB\\Documents\\HostFiles", "*.txt")
                                     .Select(path => Path.GetFileName(path))
                                     .ToArray();
            foreach (string fn in fileNames)
            {
                Console.WriteLine(fn);
            }



            Console.Write("Enter server IP: ");
            //string connectIP = Console.ReadLine();
            string connectIP = "130.70.82.148";
            Console.WriteLine(connectIP);
            master = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(connectIP), 30000);

            Random randPort = new Random();
            int myPort = randPort.Next(30000, 30500);
            //int myPort = 30001;
            string myIPString = GetLocalIPAddress();
            IPEndPoint myPeerIP = new IPEndPoint(IPAddress.Parse("130.70.82.148"), myPort);

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
                CSendFileInfo(myPort, myIPString, fileNames);
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
                        case "d":
                        case "D":

                            SocketSendString(master, "DownloadFile");
                            string output = Data_Receive2(master);
                            Console.WriteLine(output);
                            string fileRequest = Console.ReadLine();
                            SocketSendString(master, fileRequest);
                            Console.WriteLine("Retriving File Owner");
                            string HostID = Data_Receive2(master);
                            string HostIP = Data_Receive2(master);
                            string HostPort = Data_Receive2(master);
                            Console.WriteLine("HostInfo: " + HostID + " " + HostIP + " " + HostPort);
                            Thread DownloadThread = new Thread(() => DownloadFileFromHost(HostID, HostIP, HostPort, fileRequest));
                            DownloadThread.Start();
                            break;
                    }
                }
            }
            catch { }

        }

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
                    Console.WriteLine("Successful Request");
                    SocketSendString(serverSocket, "RequestAccepted");
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

        public static void CSendFileInfo(int myPort, string myIP, string[] fileNames)
        {
            Console.WriteLine("Client Preparing File Info \n Sending....");
            int index = 0;
            string ex = myPort.ToString();
            // Console.WriteLine("Attempting to send: " + ex);
            SocketSendString(master, ex);
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

        public static void DownloadFileFromHost(string hostID, string hostIP, string hostPort, string fileName)
        {
            Socket ReceiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine("Downloading from Host Client Started");
            Console.WriteLine("ClientHostID: " + hostID);

            string trueID = hostID;
            string trueIP = hostIP;
            int truePort = int.Parse(hostPort);
            string trueFileName = fileName;

            IPEndPoint hostIPEndPoint = new IPEndPoint(IPAddress.Parse(trueIP), truePort);
            ReceiverSocket.Connect(hostIPEndPoint);
            SocketSendString(ReceiverSocket, "RequestingFile");
            string acceptRetrieval = Data_Receive2(ReceiverSocket);
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



        //public static Thread ExampleRequester(Socket RequesterSocket)
        //{

        //}
    }




}