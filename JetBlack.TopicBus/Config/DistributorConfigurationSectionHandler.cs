﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;
using JetBlack.TopicBus.IO;
using log4net;

namespace JetBlack.TopicBus.Config
{
    public class DistributorConfigurationSectionHandler : IConfigurationSectionHandler
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(DistributorConfigurationSectionHandler));

        Dictionary<string, DistributorConfig> _adapters;

        public string DefaultName { get; set; }

        public DistributorConfig this[string name]
        {
            get { return _adapters.ContainsKey(name) ? _adapters[name] : null; }
        }

        public DistributorConfig DefaultConfig
        {
            get { return this[DefaultName]; }
        }

        public object Create(object parent, object configContext, XmlNode section)
        {
            if (section == null)
                return null;

            #if DEBUG
            string defaultName = "debug";
            #else
            string defaultName = "release";
            #endif
            if (section.Attributes[defaultName] == null)
            {
                Log.ErrorFormat("Unable to find configuration default \"{0}\".", defaultName);
                return null;
            }

            DefaultName = section.Attributes[defaultName].Value;
            if (string.IsNullOrEmpty(DefaultName))
            {
                Log.ErrorFormat("Unable to determine default configuration for tag \"{0}\"", defaultName);
                return null;
            }

            _adapters = CreateAdapters(section.SelectNodes("add"));
            if (_adapters == null || _adapters.Count == 0)
            {
                Log.ErrorFormat("Unable to read configuration for tag \"add\"");
                return null;
            }

            return this;
        }

        static Dictionary<string, DistributorConfig> CreateAdapters(XmlNodeList xmlNodeList)
        {
            var adapters = new Dictionary<string, DistributorConfig>();

            foreach (XmlElement xmlElement in xmlNodeList)
            {
                var adapter = CreateAdapter(xmlElement);
                if (adapter == null)
                {
                    Log.Error("Unable to read configuration for adapters");
                    return null;
                }

                adapters.Add(adapter.Name, adapter);
            }

            return adapters;
        }

        static DistributorConfig CreateAdapter(XmlElement xmlElement)
        {
            string name = xmlElement.GetAttribute("name");
            if (string.IsNullOrEmpty(name))
            {
                Log.Error("Unable to read adapter configuration for tag \"name\"");
                return null;
            }

            int port;
            if (!Int32.TryParse(xmlElement.GetAttribute("port"), out port))
            {
                Log.Error("Unable to read or understand adapter configuration for tag \"port\"");
                return null;
            }

            return new DistributorConfig(name, port);
        }
    }
}

