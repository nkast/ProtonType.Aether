#region License
//   Copyright 2021 Kastellanos Nikolaos
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
#endregion

using System;
using System.IO;

namespace nkast.ProtonType.XnaContentPipeline.ProxyServer
{
    public abstract class IPCServer : IDisposable
    {
        readonly string _uid;
        protected bool IsDisposed { get; private set; }
        protected readonly BinaryWriter Writer;
        protected readonly BinaryReader Reader;

        public IPCServer(string uid)
        {
            _uid = uid;
            Stream outStream = Console.OpenStandardOutput();
            Stream inStream = Console.OpenStandardInput();
            Writer = new BinaryWriter(outStream);
            Reader = new BinaryReader(inStream);

            bool handshakeRes = Handshake();
            if (!handshakeRes)
            {
                throw new Exception("Handshake failed");
            }
        }
        private bool Handshake()
        {
            Writer.Write("IPCServer");
            Writer.Write(_uid);
            Writer.Flush();

            string strIPCClient = Reader.ReadString();

            if (strIPCClient == "IPCClient OK")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal virtual void Run()
        {
        }

        #region Implement IDisposable

        ~IPCServer()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                // dispose managed state (managed objects)
            }
            
            IsDisposed = true;
        }

        #endregion Implement IDisposable
    }
}