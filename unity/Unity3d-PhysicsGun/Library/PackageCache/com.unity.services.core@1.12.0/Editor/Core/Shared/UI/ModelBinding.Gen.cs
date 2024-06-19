// WARNING: Auto generated code. Modifications will be lost!
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine.UIElements;

namespace Unity.Services.Core.Editor.Shared.UI
{
    class ModelBinding<TModel> : EventBinding<TModel> where TModel : INotifyPropertyChanged
    {
        readonly Dictionary<string, Action<TModel>> m_Bindings = new Dictionary<string, Action<TModel>>();

        public ModelBinding(VisualElement element) : base(element)
        {
        }

        public void BindProperty(string name, Action<TModel> update)
        {
            m_Bindings.Add(name, update);
        }

        protected override void RegisterEvent()
        {
            Source.PropertyChanged += ModelOnPropertyChanged;
        }

        protected override void UnregisterEvent()
        {
            Source.PropertyChanged -= ModelOnPropertyChanged;
        }

        protected override void UpdateAllBindings()
        {
            foreach (var binding in m_Bindings.Values)
            {
                binding(Source);
            }
        }

        void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (m_Bindings.ContainsKey(e.PropertyName))
            {
                m_Bindings[e.PropertyName](Source);
            }
        }
    }
}
