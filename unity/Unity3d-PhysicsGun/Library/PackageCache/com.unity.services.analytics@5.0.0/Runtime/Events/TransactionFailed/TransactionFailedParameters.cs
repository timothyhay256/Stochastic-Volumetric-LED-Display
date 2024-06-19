using System;

namespace Unity.Services.Analytics
{
    public struct TransactionFailedParameters
    {
        /// <summary>
        /// (Optional) The country where the transaction is taking place.
        /// If this is left null or empty, the machine's locale will be used.
        /// </summary>
        public string PaymentCountry;

        /// <summary>
        /// (Optional) The engagement ID relating to this transaction.
        /// </summary>
        public long? EngagementID;

        /// <summary>
        /// (Optional)
        /// </summary>
        public bool? IsInitiator;

        /// <summary>
        /// (Optional) The store where the transaction is taking place
        /// </summary>
        public string StoreID;

        /// <summary>
        /// (Optional) Identifies the source of the IAP
        /// </summary>
        public string StoreSourceID;

        /// <summary>
        /// (Optional) An ID that uniquely identifies the transaction
        /// </summary>
        public string TransactionID;

        /// <summary>
        /// (Optional) A unique ID for the purchased item
        /// </summary>
        public string StoreItemID;

        /// <summary>
        /// (Optional) The amazon user ID linked to this transaction
        /// </summary>
        public string AmazonUserID;

        /// <summary>
        /// (Optional) A unique ID for the SKU, linked to the store SKU ID
        /// </summary>
        public string StoreItemSkuID;

        /// <summary>
        /// (Optional) The product identifier (known as a SKU) found in the store
        /// </summary>
        public string ProductID;

        /// <summary>
        /// (Optional) The game store ID
        /// </summary>
        public string GameStoreID;

        /// <summary>
        /// (Optional) The transaction server where the transaction can be verified
        /// </summary>
        public TransactionServer? TransactionServer;

        /// <summary>
        /// (Optional) Parameter used by the IAP validation service
        /// </summary>
        public long? RevenueValidated;

        /// <summary>
        /// (Required) A name that identifies the transaction
        /// </summary>
        public string TransactionName;

        /// <summary>
        /// (Required) The type of transaction
        /// </summary>
        public TransactionType TransactionType;

        /// <summary>
        /// (Required) The products received as part of this transaction
        /// </summary>
        public Product ProductsReceived;

        /// <summary>
        /// (Required) The products spent as part of this transaction
        /// </summary>
        public Product ProductsSpent;

        /// <summary>
        /// (Required) The reason this transaction failed.
        /// </summary>
        public string FailureReason;
    }
}
