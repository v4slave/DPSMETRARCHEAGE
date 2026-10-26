using System;
class WatchCapture {
 static int Main(string[] args){var connections=new GameConnections();Capture.AcceptPacket=connections.Contains;return Capture.Run(args);}
}
