using System;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public class GroupRemarkState
    {
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
            State = "unassigned";
        }        

        public void Take()
        {
            State = "taken";
        }

        public static GroupRemarkState Create(Guid id)
            => new GroupRemarkState(id);
    }
}