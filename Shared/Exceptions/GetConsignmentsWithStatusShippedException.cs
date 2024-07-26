using System.Runtime.Serialization;

namespace Shared.Exceptions
{
    [Serializable]
    public class GetConsignmentsWithStatusShippedException : Exception
    {
        public GetConsignmentsWithStatusShippedException()
        {
        }

        public GetConsignmentsWithStatusShippedException(string? message) : base(message)
        {
        }

        public GetConsignmentsWithStatusShippedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected GetConsignmentsWithStatusShippedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}