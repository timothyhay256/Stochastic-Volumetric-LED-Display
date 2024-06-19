// WARNING: Auto generated code. Modifications will be lost!
using UnityEngine.UIElements;

namespace Unity.Services.Core.Editor.Shared.UI
{
    abstract class EventBinding<T>
    {
        public T Source { get => m_Source; set => SetEventSource(value); }

        T m_Source;
        bool m_Attached;

        protected EventBinding(VisualElement element)
        {
            element.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                m_Attached = true;
                if (Source != null)
                {
                    RegisterEvent();
                    UpdateAllBindings();
                }
            });
            element.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                m_Attached = false;
                if (Source != null)
                {
                    UnregisterEvent();
                }
            });
        }

        protected abstract void RegisterEvent();
        protected abstract void UnregisterEvent();
        protected abstract void UpdateAllBindings();

        void SetEventSource(T value)
        {
            if (m_Source != null)
            {
                UnregisterEvent();
            }
            m_Source = value;
            if (m_Source != null && m_Attached)
            {
                RegisterEvent();
                UpdateAllBindings();
            }
        }
    }
}
