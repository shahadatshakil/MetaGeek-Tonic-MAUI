
using MetaGeek.WiFi.Core.Enums;

namespace MetaGeek.WiFi.Core.Models
{
    public class InformationElement
    {
        #region Properties

        public byte[] ItsData
        {
            get;
            set;
        }

        public ushort ItsLength
        {
            get;
            set;
        }

        public InformationElementId ItsId
        {
            get;
            set;
        }

        #endregion Properties
    }
}