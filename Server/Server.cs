using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Net;

namespace Server
{
    class Server
    {

        static Socket listenerSocket;
        //filename...filePath, IPAdress, portnumber
        static Dictionary<string, List<Tuple<string, string, int>>> masterDB;

        //start server
        static void Main(string[] args)
        {
            masterDB = new Dictionary<string, List<Tuple<string, string, int>>>();
            Console.Write("Starting server: ");

            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
           
            //IPAddress = System.Net.IPAddress.Parse(Server);
            IPEndPoint serverIP = new IPEndPoint(IPAddress.Parse(GetLocalIPAddress()), 30000);
            listenerSocket.Bind(serverIP);
            Console.WriteLine(serverIP.ToString());

            Thread listenThread = new Thread(ListenThread);

            listenThread.Start();
        }

        //listener: listens for clients
        static void ListenThread()
        {
            while (true)
            {
                listenerSocket.Listen(0);
                ClientData newClient = new ClientData(listenerSocket.Accept());            
            }
        }
       

        public static void SocketSendString(Socket inSock, string input)
        {
            //Console.WriteLine("Sending: " + input);            
            input = input + "<EOF>";
            inSock.Send(Encoding.ASCII.GetBytes(input));
        }

        //with flag
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
                //Console.WriteLine("Attempting Receive");
                if (clientData.IndexOf("<EOF>") > -1)
                {
                    //Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                    //clientData.Length, clientData);
                    break;
                }
            }
            return clientData.Split(new string[]{"<EOF>"}, StringSplitOptions.None)[0];
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

        class ClientData
        {
            public Socket clientSocket;
            public Thread clientThread; //
            public string filePath;

            public ClientData(Socket clientSocket)
            {
                this.clientSocket = clientSocket;
                filePath = string.Empty;
                clientThread = new Thread(ServerFunction);
                clientThread.Start();
            }

            public void ServerFunction()
            {
                string servControl = string.Empty;

                Console.WriteLine("Client Thread started");
                //SocketSendString(clientSocket, "You are client: " + filePath);
                AcceptFileInfo();
                Console.WriteLine("Control Flow Begins");

                while(true)
                {
                    servControl = Data_Receive2(clientSocket);
                    switch(servControl)
                    {
                        case "RequestFileList":
                            Console.WriteLine("Server to Receive FileInfo");
                            SendFileInfo();
                            break;

                        case "UpdateFileServer":
                            AcceptFileInfo();
                            break;

                        case "DownloadFile":
                            Tuple<string, string, int> clientInfo;
                            Console.WriteLine("Server set to Send FileName for Download");
                            SocketSendString(clientSocket, "Type FileName with extension");
                            string receiveFile = Data_Receive2(clientSocket);
                            clientInfo = CheckFileInfo(receiveFile);
                            ////Console.WriteLine("File Owner Info: " + clientInfo.Item1.ToString() 
                            //    + " " + clientInfo.Item2.ToString() + " " 
                            //    + clientInfo.Item3.ToString());

                            Console.WriteLine("Sending FilePath: " + clientInfo.Item1.ToString());
                            SocketSendString(clientSocket, clientInfo.Item1.ToString());

                            Console.WriteLine("Sending IP: " + clientInfo.Item2.ToString());
                            SocketSendString(clientSocket, clientInfo.Item2.ToString());

                            Console.WriteLine("Sending port: " + clientInfo.Item3.ToString());
                            SocketSendString(clientSocket, clientInfo.Item3.ToString());

                            break;
                        default:
                            Console.WriteLine("Default Case");
                            //SocketSendString(clientSocket, "In Default Case Currently");
                            break;
                    }
                }
            }

            public void AcceptFileInfo()
            {
                Console.WriteLine("AcceptFileInfo Started");
                bool received = false;
                //receive port number from peer
                int myPort = int.Parse(Data_Receive2(clientSocket));
                Console.WriteLine(myPort.ToString());
                while(true)
                {
                    if(myPort != 0 && received == false)
                    {
                       
                        int fileIndex = 0;
                        //Console.WriteLine("Attempting rec1 send to client: " + filePath);
                        SocketSendString(clientSocket, "SERVER: PortNumber has been Received, Proceeding...");
                        received = true;
                       // Console.WriteLine("Stage 2: IPAddress Retrieval");
                        string peerIP = string.Empty;
                        peerIP = Data_Receive2(clientSocket);
                        SocketSendString(clientSocket, "SERVER: IPAddress Received, Proceeding...");
                        string fileName = string.Empty;
                        string filePath = string.Empty;
                        
                        //Console.WriteLine("Stage 3: FileName Retrieval");
                        int fileCount = int.Parse(Data_Receive2(clientSocket));
                        //Console.WriteLine("FileRetrieve Started over " + fileCount.ToString() + " files");
                        while (fileIndex < fileCount)
                        {
                            fileName = Data_Receive2(clientSocket);
                            Console.WriteLine("FileName: " + fileName);
                            SocketSendString(clientSocket, "SendFilePath");
                            filePath = Data_Receive2(clientSocket);
                            Console.WriteLine("FilePath: " + filePath);
                            
                            var tupInfo = new Tuple<string, string, int>(filePath, peerIP, myPort);
                            
                            //Console.WriteLine(fileinfolist.ToString());
                            if(masterDB.ContainsKey(fileName))
                            {
                                List<Tuple<string, string, int>> tupList = masterDB[fileName];
                                foreach(var tup in tupList)
                                {
                                    if(tupInfo.Item1 == tup.Item1 
                                        && tupInfo.Item2 == tup.Item2 
                                        && tupInfo.Item3 == tup.Item3 )
                                    {
                                        Console.WriteLine("Duplicate FileInfo Found, Not Adding to Server List");
                                    }
                                    else
                                    {
                                        Console.WriteLine("Uniqe FileInfo Found, Adding to Server List");
                                        masterDB[fileName].Add(tupInfo);
                                    }
                                }
                                //masterDB[fileName].Add(tupInfo);
                                Console.WriteLine(fileName.ToString());
                            }
                            else
                            {
                                masterDB[fileName] = new List<Tuple<string,string,int>>();
                                masterDB[fileName].Add(tupInfo);

                                foreach (Tuple<string, string, int> tup in masterDB[fileName])
                                {
                                    Console.WriteLine(tup.Item1.ToString() + " " + tup.Item2 + " " + tup.Item3);
                                }
                      
                            }
                            
                            fileIndex = fileIndex + 1;
                            

                        }
                        //Console.WriteLine("Successful FileInfo Transmission from Client");
                        
                        break;
                    }
                    break;
                }
            }

            public void SendFileInfo()
            {
                Console.WriteLine("Sending All Current Filenames to Client: " + filePath);
                SocketSendString(clientSocket, masterDB.Keys.Count.ToString());
                foreach(string varFileName in masterDB.Keys)
                {
                    Console.WriteLine(varFileName);
                    SocketSendString(clientSocket, varFileName);
                }
            }

            public Tuple<string, string, int> CheckFileInfo(string fileChoice)
            {
                
                Console.WriteLine("FileRequest: " + fileChoice);
            
                //Console.WriteLine(masterDB[fileChoice] == null?"file does not exist" : "file exists in database");
                foreach(Tuple<string, string, int> clientTuple in masterDB[fileChoice])
                {
                    Console.WriteLine("Returning with Valid Client Info");
                    return clientTuple;
                }
                Console.WriteLine("Returning Null");
                return null;
                
            }

            
        }
    }
}
