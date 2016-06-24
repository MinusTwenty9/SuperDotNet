using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SuperDotNet.SComp
{
    public class NodeInfo
    {
        public string id;
        public string client_id;

        public NodeInfo(string id)
        {
            this.id = id;
        }

        public NodeInfo(string id, string client_id)
        {
            this.id = id;
            this.client_id = client_id;
        }
    }
}
