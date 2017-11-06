using System;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public class GroupRemarkState
    {
        private static readonly string DeniedState = "denied";
        public Guid Id { get; protected set; }
        public string State { get; protected set; }

        protected GroupRemarkState()
        {
        }

        protected GroupRemarkState(Guid id)
        {
            Id = id;
            State = null;
        }

        public void Assign()
        {
            State = "assigned";
        }

        public void Deny()
        {
            State = DeniedState;
        }        

        public void Take()
        {
            if (State == DeniedState)
            {
                return;
            }            
            State = "taken";
        }

        public void Clear()
        {
            if (State == DeniedState)
            {
                return;
            }
            State = null;
        }

        public static GroupRemarkState Create(Guid id)
            => new GroupRemarkState(id);
    }
}