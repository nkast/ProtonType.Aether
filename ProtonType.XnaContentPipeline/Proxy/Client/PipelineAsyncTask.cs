#region License
//   Copyright 2021-2025 Kastellanos Nikolaos
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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace nkast.ProtonType.XnaContentPipeline.ProxyClient
{
    internal abstract class PipelineAsyncTaskBase
    {
        public readonly Guid Guid;
        public readonly IProxyLogger Logger;

        public PipelineAsyncTaskBase(Guid contextGuid, IProxyLogger logger)
        {
            this.Guid = contextGuid;
            this.Logger = logger;
        }

        internal void InternalOnSucceeded()
        {
            OnSucceeded();
        }

        internal void InternalOnFailed()
        {
            OnFailed();
        }

        protected abstract void OnSucceeded();
        protected abstract void OnFailed();

    }

    internal abstract class PipelineAsyncTask<TResult> : PipelineAsyncTaskBase
    {
        private readonly TaskCompletionSource<TResult> _taskCompletionSource;

        protected TaskCompletionSource<TResult> TaskCompletionSource
        {
            get { return _taskCompletionSource; }
        }

        public Task<TResult> Task
        {
            get { return _taskCompletionSource.Task; }
        }

        public PipelineAsyncTask(Guid contextGuid, IProxyLogger logger)
            : base(contextGuid, logger)
        {
            this._taskCompletionSource = new TaskCompletionSource<TResult>();
        }

        protected override void OnFailed()
        {
            _taskCompletionSource.SetException(new Exception("Task failed."));
        }
    }

    internal class PipelineAsyncTask : PipelineAsyncTaskBase
    {
#if NET8_0_OR_GREATER
        private readonly TaskCompletionSource _taskCompletionSource;
#else
        private readonly TaskCompletionSource<object> _taskCompletionSource;
#endif

        public Task Task
        {
            get { return _taskCompletionSource.Task; }
        }

        public PipelineAsyncTask(Guid contextGuid, IProxyLogger logger)
            : base(contextGuid, logger)
        {
#if NET8_0_OR_GREATER
            this._taskCompletionSource = new TaskCompletionSource();
#else
            this._taskCompletionSource = new TaskCompletionSource<object>();
#endif
        }

        protected override void OnSucceeded()
        {
#if NET8_0_OR_GREATER
            this._taskCompletionSource.SetResult();
#else
            this._taskCompletionSource.SetResult(null);
#endif
        }

        protected override void OnFailed()
        {
            _taskCompletionSource.SetException(new Exception("Task failed."));
        }

    }
}
