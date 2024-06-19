// WARNING: Auto generated code. Modifications will be lost!
using System;
using System.Collections.Specialized;
using Unity.Services.Core.Editor.Shared.Infrastructure.Collections;
using UnityEngine.UIElements;

namespace Unity.Services.Core.Editor.Shared.UI
{
    class CollectionBinding<TItem> : EventBinding<IReadOnlyObservable<TItem>>
    {
        Action<IReadOnlyObservable<TItem>, NotifyCollectionChangedEventArgs> m_CollectionChanged;

        public CollectionBinding(VisualElement element) : base(element)
        {
        }

        public void BindCollectionChanged(Action<IReadOnlyObservable<TItem>, NotifyCollectionChangedEventArgs> collectionChanged)
        {
            m_CollectionChanged = collectionChanged;
        }

        protected override void RegisterEvent()
        {
            Source.CollectionChanged += OnCollectionChanged;
        }

        protected override void UnregisterEvent()
        {
            Source.CollectionChanged -= OnCollectionChanged;
        }

        protected override void UpdateAllBindings()
        {
            m_CollectionChanged?.Invoke(Source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            m_CollectionChanged?.Invoke(Source, e);
        }
    }
}
