// WARNING: Auto generated code. Modifications will be lost!
using System;
using System.Timers;

namespace Unity.Services.Core.Editor.Shared.EditorUtils
{
    class EditorValueTracker<T> : IDisposable
    {
        public EventHandler<T> ValueChanged;
        Func<T> m_Getter;
        T m_PreviousValue;
        Timer m_Timer;

        public T Value => m_Getter();

        public EditorValueTracker(Func<T> getter, float checkInterval = 500f)
        {
            m_Getter = getter;
            m_PreviousValue = m_Getter();
            m_Timer = new Timer()
            {
                Interval = checkInterval
            };

            m_Timer.Elapsed += (_a, _b) => Sync.RunNextUpdateOnMain(TrackValue);
            m_Timer.Start();
        }

        void TrackValue()
        {
            var currentValue = m_Getter();

            if (!m_PreviousValue.Equals(currentValue))
            {
                ValueChanged?.Invoke(this, currentValue);
                m_PreviousValue = currentValue;
            }
        }

        public void Dispose()
        {
            m_Timer.Dispose();
        }
    }
}
