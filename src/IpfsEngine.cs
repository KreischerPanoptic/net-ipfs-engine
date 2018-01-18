﻿using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Ipfs.CoreApi;
using Ipfs.Engine.CoreApi;
using Ipfs.Engine.Cryptography;
using System.Reflection;

namespace Ipfs.Engine
{
    /// <summary>
    ///   TODO
    /// </summary>
    public partial class IpfsEngine : ICoreApi
    {
        static ILog log = LogManager.GetLogger(typeof(IpfsEngine));

        bool repositoryInited;
        KeyChain keyChain;
        Peer localPeer = new Peer();
        char[] passphrase;

        /// <summary>
        ///   Creates a new instance of the <see cref="IpfsEngine"/> class.
        /// </summary>
        public IpfsEngine(char[] passphrase)
        {
            this.passphrase = passphrase;

            // Init the core api inteface.
            Bitswap = new BitswapApi(this);
            Block = new BlockApi(this);
            Config = new ConfigApi(this);
            Dag = new DagApi(this);
            Dht = new DhtApi(this);
            FileSystem = new FileSystemApi(this);
            Generic = new GenericApi(this);
            Key = new KeyApi(this);
            Name = new NameApi(this);
            Object = new ObjectApi(this);
            Pin = new PinApi(this);
            PubSub = new PubSubApi(this);
            Swarm = new SwarmApi(this);
        }

        /// <summary>
        ///   The configuration options.
        /// </summary>
        public IpfsEngineOptions Options { get; set; } = new IpfsEngineOptions();

        /// <inheritdoc />
        public IBitswapApi Bitswap { get; private set; }

        /// <inheritdoc />
        public IBlockApi Block { get; private set; }

        /// <inheritdoc />
        public IConfigApi Config { get; private set; }

        /// <inheritdoc />
        public IDagApi Dag { get; private set; }

        /// <inheritdoc />
        public IDhtApi Dht { get; private set; }

        /// <inheritdoc />
        public IFileSystemApi FileSystem { get; private set; }

        /// <inheritdoc />
        public IGenericApi Generic { get; private set; }

        /// <inheritdoc />
        public IKeyApi Key { get; private set; }

        /// <inheritdoc />
        public INameApi Name { get; private set; }

        /// <inheritdoc />
        public IObjectApi Object { get; private set; }

        /// <inheritdoc />
        public IPinApi Pin { get; private set; }

        /// <inheritdoc />
        public IPubSubApi PubSub { get; private set; }

        /// <inheritdoc />
        public ISwarmApi Swarm { get; private set; }

        internal async Task<Repository> Repository(CancellationToken cancel = default(CancellationToken))
        {
            Repository repo = new Repository
            {
                Options = Options.Repository
            };

            if (repositoryInited)
            {
                return await Task.FromResult(repo);
            }

            ;
            lock (this)
            {
                if (!repositoryInited)
                {
                    repositoryInited = true;
                }
            }
            await repo.CreateAsync(cancel);
            return repo;
        }

        /// <summary>
        ///   Provides access to the <see cref="KeyChain"/>.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the <see cref="keyChain"/>.
        /// </returns>
        public async Task<KeyChain> KeyChain(CancellationToken cancel = default(CancellationToken))
        {
            if (keyChain == null)
            {
                lock (this)
                {
                    if (keyChain == null)
                    {
                        keyChain = new KeyChain(this)
                        {
                            Options = Options.KeyChain
                        };
                     }
                }

                await keyChain.SetPassphraseAsync(passphrase);
                
                // Maybe create "self" key, this is the local peer's id.
                var self = await keyChain.FindKeyByNameAsync("self", cancel);
                if (self == null)
                {
                    self = await keyChain.CreateAsync("self", null, 0, cancel);
                }
            }
            return keyChain;
        }

        /// <summary>
        ///   Provides access to the local peer.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   a <see cref="Peer"/>.
        /// </returns>
        public async Task<Peer> LocalPeer(CancellationToken cancel = default(CancellationToken))
        {
            if (localPeer.Id == null)
            {
                var keyChain = await KeyChain(cancel);
                var self = await keyChain.FindKeyByNameAsync("self", cancel);
                localPeer.Id = self.Id;
                localPeer.ProtocolVersion = "ipfs/0.1.0";
                var version = typeof(IpfsEngine).GetTypeInfo().Assembly.GetName().Version;
                localPeer.AgentVersion = $"net-ipfs/{version.Major}.{version.Minor}.{version.Revision}";
            }
            return localPeer;
        }
    }
}