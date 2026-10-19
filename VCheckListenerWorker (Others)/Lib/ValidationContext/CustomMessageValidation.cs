using NHapi.Base.Model;
using NHapi.Base.Validation;
using NHapi.Base.Validation.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCheckListenerWorker_Others.Lib.ValidationContext
{
    public class CustomMessageValidation : IValidationContext
    {
        public IList<IMessageRule> MessageRules { get; } = new List<IMessageRule>();

        public CustomMessageValidation()
        {
            MessageRules.Add(new EDDataTypeValidationContext());
        }

        public IPrimitiveTypeRule[] getPrimitiveRules(string theVersion, string theTypeName, IPrimitive theType)
        {
            return new IPrimitiveTypeRule[0];
        }

        public IPrimitiveTypeRule[] GetPrimitiveRules(string theVersion, string theTypeName, IPrimitive theType)
        {
            return new IPrimitiveTypeRule[0];
        }

        public IMessageRule[] getMessageRules(string theVersion, string theMessageType, string theTriggerEvent)
        {
            return new IMessageRule[0];
        }

        public IMessageRule[] GetMessageRules(string theVersion, string theMessageType, string theTriggerEvent)
        {
            return new IMessageRule[0];
        }

        public IEncodingRule[] getEncodingRules(string theVersion, string theEncoding)
        {
            return new IEncodingRule[0];
        }

        public IEncodingRule[] GetEncodingRules(string theVersion, string theEncoding)
        {
            return new IEncodingRule[0];
        }
    }
}
