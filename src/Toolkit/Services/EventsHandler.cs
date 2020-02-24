﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Xarial.XCad.Toolkit.Services
{
    /// <summary>
    /// Utility allowing to implement lazy event handlers for a wrapped events
    /// </summary>
    /// <typeparam name="TDel">Delegate type of the wrapped event</typeparam>
    /// <remarks>Use this approach when handling of events might result in the performance penalties or other issues
    /// so it is only required to subscribe to events when underlying users are subscribed</remarks>
    public abstract class EventsHandler<TDel> : IDisposable
        where TDel : Delegate
    {
        protected const int S_OK = 0;
        protected const int S_FALSE = 1;

        public TDel Delegate { get; set; }

        private bool m_IsSubscribed;

        protected EventsHandler()
        {
            m_IsSubscribed = false;
        }

        public void Attach(TDel del)
        {
            SubscribeIfNeeded();
            AddToInvocationList(del);
        }

        public void Detach(TDel del)
        {
            RemoveFromInvocationList(del);

            if (Delegate == null)
            {
                UnsubscribeIfNeeded();
            }
        }

        private void SubscribeIfNeeded()
        {
            if (!m_IsSubscribed)
            {
                SubscribeEvents();

                m_IsSubscribed = true;
            }
        }

        private void UnsubscribeIfNeeded()
        {
            if (m_IsSubscribed)
            {
                UnsubscribeEvents();

                m_IsSubscribed = false;
            }
        }

        public void Dispose()
        {
            UnsubscribeIfNeeded();
        }

        protected abstract void SubscribeEvents();
        protected abstract void UnsubscribeEvents();

        private void AddToInvocationList(TDel del)
        {
            Delegate = System.Delegate.Combine(Delegate, del) as TDel;
        }
        
        private void RemoveFromInvocationList(TDel del)
        {
            Delegate = System.Delegate.Remove(Delegate, del) as TDel;
        }
    }
}