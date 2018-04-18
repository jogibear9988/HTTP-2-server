﻿using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using lib;

namespace ExampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var serverCertificate = new X509Certificate2("Certificate/TcpTLSServer_TemporaryKey.pfx", "1234");
            Server server = new Server("127.0.0.1", serverCertificate);

            /*
            //Zip test
            DirectoryInfo di = new DirectoryInfo(@"C:\Users\Martin Wangen\source\repos\HTTP-2-server\ExampleServer\WebApp\");
            FileInfo fi = new FileInfo(@"C:\Users\Martin Wangen\source\repos\HTTP-2-server\ExampleServer\WebApp\temp\index.html.gz");
            ZipStream.Compress(di);
            ZipStream.Decompress(fi);


            Console.ReadLine();
            */
        }
    }
}
