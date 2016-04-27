This is my implementation for my Peer to Peer file hosting program with a connection manager:

This Solution has two executables that is:

	Server
	Client

The server will retrieve the local machine's IP address and designate the port # to 30000, when it is started. The opens up new thread for each individual client.
The control flow for the server is handled inside of the client threads the server hosts, the Master Database will be updated via each thread.


The Client executable will need the user to input which his/her hosting directory would be, and where their receiving directory would be, both can be the same.

The client is then required to input the server's IP, which then it connects successfully, the client is able to upload their files from their hosting directory and able to receive files from other peers connected at this time to their receiving directory.

All of the control flow is handled via the clients, because of the inherent nature of the server threading, clients can have unexpected disconnects and the flowthrough for file removal will work properly as a regular synchronous disconnect.

Flow Control for Client                

		Retreive FileInfo from Server = R
                Update your FileInfo to Server = U
                Download file from server's Clients = D
                Print Serverside the Master Database = P
                Disconnect from Server, removes FileData = Q