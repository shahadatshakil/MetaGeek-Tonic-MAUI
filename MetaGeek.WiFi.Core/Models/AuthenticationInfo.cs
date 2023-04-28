using System.Collections.Generic;

namespace MetaGeek.WiFi.Core.Models
{
    public class AuthenticationInfo
    {
        public int ItsAuthCount { get; set; }

        public bool ItsAuthenticationFlag { get; set; }

        public bool ItsEapFrameFlag { get; set; }

        public bool ItsRoamingFlag { get; set; }

        public bool ItsReassociationFrameFlag { get; set; }

        public LinkedListNode<PacketMetaData> ItsStartingPacket { get; set; }

        public AuthenticationInfo()
        {
            ItsAuthCount = 0;
            ItsAuthenticationFlag = false;
            ItsEapFrameFlag = false;
            ItsRoamingFlag = false;
            ItsReassociationFrameFlag = false;
            ItsStartingPacket = null;
        }
    }
}
