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
using System.Diagnostics;
using System.IO;

namespace nkast.ProtonType.XnaContentPipeline.ProxyClient
{
    public abstract class IPCClient : IDisposable
    {
        protected bool IsDisposed { get; private set; }
        protected readonly BinaryWriter Writer;
        protected readonly BinaryReader Reader;
        string _uid;

        public IPCClient(string serverFilename)
        {
            _uid = Guid.NewGuid().ToString("N");

            var info = new ProcessStartInfo();
            info.FileName = serverFilename;
            info.Arguments = "IPCServer" + " " + _uid;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            var other = Process.Start(info);

            Writer = new BinaryWriter(other.StandardInput.BaseStream);
            Reader = new BinaryReader(other.StandardOutput.BaseStream);

            bool handshakeRes = Handshake();
            if (!handshakeRes)
            {
                throw new Exception("Handshake failed");
            }
        }

        private bool Handshake()
        {
            var strIPCServer = Reader.ReadString();
            var strUid = Reader.ReadString();

            if (strIPCServer == "IPCServer" && strUid == _uid)
            {
                Writer.Write("IPCClient OK");
                Writer.Flush();
                return true;
            }
            else
            {
                return false;
            }
        }

        #region Implement IDisposable

        ~IPCClient()
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