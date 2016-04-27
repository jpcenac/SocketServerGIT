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
            Console.Write("Starting server on: ");

            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint serverIP = new IPEndPoint(IPAddress.Parse(GetLocalIPAddress()), 30000);
            listenerSocket.Bind(serverIP);
            Console.WriteLine(serverIP.ToString() + "\n Server Port:: " + "30000");

            Thread listenThread = new Thread(ListenThread);

            listenThread.Start();
        }

        //listener: listens for clients to upload their host info to become peers
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
            try
            {
                //Console.WriteLine("Sending: " + input);            
                input = input + "<EOF>";
                inSock.Send(Encoding.ASCII.GetBytes(input));
            }
            catch
            {

                Console.WriteLine("Error Occured...Unable to send String");
            }
        }

        //with flag
        public static string Data_Receive2(object cSocket)
        {
            string clientData = null;
            Socket clientSocket = (Socket)cSocket;
            byte[] Buffer;

            try
            {
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
                return clientData.Split(new string[] { "<EOF>" }, StringSplitOptions.None)[0];
            }
            catch
            {
                // Console.WriteLine(e);


                return "UnexpectedDisc";
            }
            //return clientData.Split(new string[] { "<EOF>" }, StringSplitOptions.None)[0];
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
            private string thisClientIP;
            private int thisClientPort;

            public ClientData(Socket clientSocket)
            {
                this.clientSocket = clientSocket;
                filePath = string.Empty;
                clientThread = new Thread(ServerFunction);
                clientThread.Start();
            }

            public void ServerFunction()
            {
                try
                {
                    string servControl = string.Empty;
                    string parseClientInfo = Data_Receive2(clientSocket);
                    string[] clientIPPort = parseClientInfo.Split(';');
                    thisClientIP = clientIPPort[0];
                    thisClientPort = int.Parse(clientIPPort[1]);
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Client IP::: " + thisClientIP);
                    Console.WriteLine("Client Port: " + thisClientPort.ToString());
                    Console.WriteLine("Client Has Connected");
                    Console.ResetColor();

                    while (true)
                    {
                        servControl = Data_Receive2(clientSocket);
                        switch (servControl)
                        {
                            case "RequestFileList":
                                SendFileInfo();
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.BackgroundColor = ConsoleColor.White;
                             
                                Console.WriteLine("Sent all Current File Information...Returning");
                                Console.ResetColor();

                                break;

                            case "UpdateFileServer":
                                RemoveFileInfo(thisClientIP, thisClientPort.ToString());
                                
                                AcceptFileInfo();
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.BackgroundColor = ConsoleColor.White;
                                Console.WriteLine("FileServer Updated HostInfo...Returning");
                                Console.ResetColor();
                                break;

                            case "PrintDataBase":
                                CheckDatabase();
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.BackgroundColor = ConsoleColor.White;
                            
                                Console.WriteLine("Entire Server DataBase Printed...Returning");
                                Console.ResetColor();
                                break;

                            case "DownloadFile":
                                try
                                {
                                    Tuple<string, string, int> clientInfo;
                                    Console.WriteLine("Server set to Send FileName for Download");
                                    SocketSendString(clientSocket, "SERVER//Type FileName with extension");
                                    string receiveFile = Data_Receive2(clientSocket);

                                    if (masterDB.Keys.Contains(receiveFile) & masterDB[receiveFile].Count == 1)
                                    {
                                        SocketSendString(clientSocket, "Correct");
                                        clientInfo = CheckFileInfoSingle(receiveFile);
                                        string HostInfo = stringifyTuple(clientInfo);
                                        SocketSendString(clientSocket, HostInfo);
                                        Console.WriteLine("FileRequest Success, Returning to Control Flow");
                                        break;
                                    }
                                    else if (masterDB.Keys.Contains(receiveFile) & masterDB[receiveFile].Count > 1)
                                    {
                                       
                                        SocketSendString(clientSocket, "Correct+");
                                        string continueSt = Data_Receive2(clientSocket);
                                        Console.WriteLine(continueSt);
                                        SocketSendString(clientSocket, masterDB[receiveFile].Count().ToString());
                                        string ConfirmGo = Data_Receive2(clientSocket);
                                        Console.WriteLine(ConfirmGo);
                                       
                                        CheckFileInfoMult(receiveFile);
                                        Console.WriteLine("FileRequest Success, Returning to Control Flow");

                                        break;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Client Entered Incorrect Filename or Error Occured");
                                        SocketSendString(clientSocket, "Incorrect");
                                    }

                                }
                                catch 
                                {
                                    Console.WriteLine("Error Occured Between Client server, Returning Unsuccessfully");
                                    break;
                                }
                                break;
                            case "Disconnecting":
                                Console.WriteLine("User is Disconnecting, removing their Host Info");
                                RemoveFileInfo(thisClientIP, thisClientPort.ToString());
                                Thread.Sleep(1000);
                                SocketSendString(clientSocket, "RemoveFileInfoSuccess");
                                clientSocket.Close();
                                Console.WriteLine("Client at" +  thisClientIP + " Has Disconnected");
                                Console.WriteLine("WAWRNING:::::::Aborting Client Thread:::::::WARNING", Console.ForegroundColor = ConsoleColor.Red);
                                Thread.CurrentThread.Abort();
                                break;

                            case "UnexpectedDisc":
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("ERROR!!::Socket was Probably Removed Forcibly");
                                Console.WriteLine("::::::Attempting to Removing Client Info::::::");
                                Console.WriteLine("C IP:::" + thisClientIP);
                                Console.WriteLine("C Port: " + thisClientPort.ToString());
                                RemoveFileInfo(thisClientIP, thisClientPort.ToString());
                                Thread.Sleep(1000);
                                clientSocket.Shutdown(SocketShutdown.Both);
                                clientSocket.Close();
                                Thread.CurrentThread.Abort();
                                break;
                            default:
                                Console.WriteLine("Default Case");
                                //SocketSendString(clientSocket, "In Default Case Currently");
                                break;
                        }
                    }
                }
                catch
                {
                    //Console.WriteLine(e);
                    SocketSendString(clientSocket, "ErrorOccured");
                    Console.WriteLine("Aborting ClientThread, Connection to Client Lost");
                    clientSocket.Close();
                    Thread.CurrentThread.Abort();
                    return;
                }
            }

            public void AcceptFileInfo()
            {
               
                string fileCountString = Data_Receive2(clientSocket);

                int fileCount = int.Parse(fileCountString);
                SocketSendString(clientSocket, "FileCountReceived");

                for (int i = 0; i < fileCount; i++)
                {
                    string fileName = Data_Receive2(clientSocket);
                    //Console.WriteLine(fileName);
                    SocketSendString(clientSocket, "FNReceived");
                    string filePath = Data_Receive2(clientSocket);
                    //Console.WriteLine(filePath);
                    SocketSendString(clientSocket, "FPReceived");
                    string commenceCheck = Data_Receive2(clientSocket);
                    List<Tuple<string, string, int>> fileInfoList = new List<Tuple<string, string, int>>();
                    Tuple<string, string, int> fileInfo = new Tuple<string, string, int>(filePath, thisClientIP, thisClientPort);
                    //Console.WriteLine(commenceCheck);
                    if (!masterDB.ContainsKey(fileName))
                    {
                        fileInfoList.Clear();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("NEW FILE::Adding " + fileName + " to Server Database");
                        Console.ResetColor();

                        //Console.WriteLine(fileName + ": " + fileInfo.Item1 
                        //    + "\nIP:: " + fileInfo.Item2 
                        //    + "\nPort:: " + fileInfo.Item3.ToString());
                        Console.WriteLine("HOST INFO:::::::::::::::::::::::::");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("FilePath:: ");
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine(fileInfo.Item1);

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("PeerIP:::: ");
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine(fileInfo.Item2);

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("PeerPort:: ");
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine(fileInfo.Item3.ToString());
                        fileInfoList.Add(fileInfo);
                        masterDB.Add(fileName, fileInfoList);
                        Console.ResetColor();
                    }
                    else
                    {
                       
                        //Console.WriteLine(fileName);
                        List<Tuple<string, string, int>> dupTupleList = masterDB[fileName];

                        foreach (Tuple<string, string, int> compTuple in masterDB[fileName].ToList())
                        {

                            if (fileInfo.Item1 == compTuple.Item1 && fileInfo.Item2 == compTuple.Item2 && fileInfo.Item3 == compTuple.Item3)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Duplicate File Found");
                                masterDB[fileName] = masterDB[fileName].Distinct().ToList();
                                Console.ForegroundColor = ConsoleColor.White;

                            }
                            else if (fileInfo.Item1 != compTuple.Item1 & fileInfo.Item2 != compTuple.Item2 || fileInfo.Item3 != compTuple.Item3)
                            //if(!masterDB[fileName].Contains(compTuple))
                            {
                                if (fileInfo.Item1 != compTuple.Item1)
                                {
                                    Console.ForegroundColor = ConsoleColor.DarkRed;
                                    Console.WriteLine("Different FilePaths");
                                    Console.ResetColor();

                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("Adding new Host Info to this FileName");
                                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                                    Console.Write("FilePath::::::::::::");
                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                    Console.WriteLine(fileInfo.Item1);

                                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                                    Console.Write("HostIP::::::::::::::");
                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                    Console.WriteLine(fileInfo.Item2);

                                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                                    Console.Write("HostPort::::::::::::");
                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                    Console.WriteLine(fileInfo.Item3.ToString());

                                    masterDB[fileName].Add(fileInfo);
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.Write(fileName);
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine(" host count::" + masterDB[fileName].Count.ToString() + "\n");
                                    break;
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                                    Console.WriteLine("Different ClientIP and/or ClientPORT");
                                    Console.ResetColor();

                                    Console.ForegroundColor = ConsoleColor.Green;
                                    
                                    Console.WriteLine("Adding new Host Info to this FileName");
                                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                                    Console.Write("FilePath::::::::::::");
                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                    Console.WriteLine(fileInfo.Item1);

                                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                                    Console.Write("HostIP::::::::::::::");
                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                    Console.WriteLine(fileInfo.Item2);

                                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                                    Console.Write("HostPort::::::::::::");
                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                    Console.WriteLine(fileInfo.Item3.ToString());

                                    masterDB[fileName].Add(fileInfo);
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.Write(fileName);
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine(" has " + masterDB[fileName].Count.ToString() + "Hosts Currently" + "\n");
                                    
                                    Console.ResetColor();
                                    break;
                                }

                            }
                            else
                            {
                                Console.WriteLine("Doing Nothing here");
                            }
                        }
                    }
                }
            }

            public void CheckDatabase()
            {
                foreach (string fn in masterDB.Keys)
                {
                    Console.ForegroundColor = ConsoleColor.Green;                    
                    Console.WriteLine("DB has " + masterDB[fn].Count.ToString() + " host(s) on Record for");
                    Console.Write("::::::::::::::::::::::::::::::::: ");
                    PrintCyan(fn);
                    Console.WriteLine();
                    foreach (Tuple<string, string, int> tupPrint in masterDB[fn].ToList())
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(" FilePath:: ");
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine(tupPrint.Item1);

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(" PeerIP:::: ");
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine(tupPrint.Item2);

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(" PeerPort:: ");
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine(tupPrint.Item3.ToString());

                        Console.WriteLine();
                    }
                }
            }

            public void SendFileInfo()
            {
                Console.WriteLine("Request for File List Received");
                SocketSendString(clientSocket, "SERVER: SendingFileNames");
                string readyConfirm = Data_Receive2(clientSocket);
                string FileList = string.Empty;
                if(masterDB.Keys.Count > 0 )
                {
                    foreach (string fn in masterDB.Keys)
                    {
                        //Console.WriteLine("Sending: " + fn);
                        FileList = FileList + string.Concat(fn, ":");

                    }
                }
                else
                {
                    FileList = "Server has no FileNames in Database currently";
                }
               
                //Console.WriteLine(FileList);
                SocketSendString(clientSocket, FileList);
            }

            public Tuple<string, string, int> CheckFileInfoSingle(string fileChoice)
            {
                Console.WriteLine("FileRequest: " + fileChoice);
                int index = 0;
                //Console.WriteLine(masterDB[fileChoice] == null?"file does not exist" : "file exists in database");
                foreach (Tuple<string, string, int> clientTuple in masterDB[fileChoice])
                {
                    Tuple<string, string, int>[] fileNameArr = masterDB[fileChoice].ToArray();
                    Console.WriteLine("IP:::: " + clientTuple.Item2);
                    Console.WriteLine("Port:: " + clientTuple.Item3.ToString());
                    Console.WriteLine("Index: " + fileNameArr[index].ToString());
                    index += 1;
                    if (masterDB[fileChoice].Count == 1)
                    {
                        Console.WriteLine("Returning with Valid Client Info");
                        return clientTuple;
                    }
                    
                    return clientTuple;
                }
                Console.WriteLine("Returning Null");
                return null;
            }

            public void CheckFileInfoMult(string fileChoice)
            {

                Console.WriteLine("FileRequest: " + fileChoice);
                Dictionary<int, Tuple<string, string, int>> indexedTuple = new Dictionary<int, Tuple<string, string, int>>();

                Console.WriteLine("File Hosts Count: " + masterDB[fileChoice].Count);
                Tuple<string, string, int>[] fileChoiceArray = masterDB[fileChoice].ToArray();
                string[] serializeTuple = new string[fileChoiceArray.Length];
                for (int index = 0; index < masterDB[fileChoice].Count; index++)
                {
                    string ccIndexIP = String.Concat(index, ";", fileChoiceArray[index].Item1, ";", fileChoiceArray[index].Item2);
                    string serializeKVP = String.Concat(ccIndexIP, ";", fileChoiceArray[index].Item3.ToString());
                    Console.WriteLine("Sending: " + serializeKVP) ;
                    SocketSendString(clientSocket, serializeKVP);
                    indexedTuple.Add(index, fileChoiceArray[index]);
                    Thread.Sleep(100);
                }

                //Console.WriteLine("\\\\////\\\\////\\\\////\\\\////\\\\////\\\\////");
                SocketSendString(clientSocket, "Choose Index::: ");
                int indexChoice = int.Parse(Data_Receive2(clientSocket));
                Console.WriteLine("Client's Index Choicee: " +indexChoice.ToString());

            }

            public string stringifyTuple(Tuple<string, string, int> cInfo)
            {
                string filePath = cInfo.Item1;
                string ipAddress = cInfo.Item2;
                string portNum = cInfo.Item3.ToString();
                string ipAndPort = String.Concat(ipAddress, ";", portNum);

                string allClietInfo = String.Concat(filePath, ";", ipAndPort);

                return allClietInfo;
            }

            public void RemoveFileInfo(string clientIP, string clientPort)
            {
                int clientPortInt = int.Parse(clientPort);
                    foreach (string fileName in masterDB.Keys)
                    {
                        List<Tuple<string, string, int>> copyList = masterDB[fileName];

                        foreach (Tuple<string, string, int> targetTuple in copyList)
                        {
                            //Console.WriteLine("REMOVING HOST FROM LIST FOR FILE:::::::: " + fileName);

                            if (targetTuple.Item2 == clientIP && targetTuple.Item3 == clientPortInt)
                            {

                                if (masterDB[fileName].Count > 1)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("DB has " + masterDB[fileName].Count.ToString() + " on Record");
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write(" REMOVING");
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.Write(" IP::::: ");
                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                    Console.WriteLine(clientIP);

                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write(" REMOVING");
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.Write(" PORT::: ");
                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                    Console.WriteLine(clientPort);

                                    Console.ForegroundColor = ConsoleColor.Green;

                                    Console.WriteLine("FileName: " + fileName + " remains");
                                    masterDB[fileName].Remove(targetTuple);
                                    //RemoveFileInfo(clientIP, clientPort);
                                    Console.ResetColor();
                                    break;

                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("FILENAME::::::::::: " + fileName);
                                    Console.Write(" REMOVING");
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.Write(" IP::::: ");
                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                    Console.WriteLine(clientIP);

                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write(" REMOVING");
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.Write(" PORT::: ");
                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                    Console.WriteLine(clientPort);


                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Last Host for this File Removed, " + fileName + " Removed from DataBase \n \n");

                                    masterDB.Remove(fileName);
                                    RemoveFileInfo(clientIP, clientPort);
                                    Console.ResetColor();
                                    
                                }

                            }
                            else
                            {
                                //Console.WriteLine("IP and Port DO NOT MATCH, CONTINUING ");
                            }
                        }

                        

                }
                    
                
            }

            public string PrintCyan(string input)
            {
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(input);
                Console.ResetColor();
                return input;
            }

        }
    }
}
