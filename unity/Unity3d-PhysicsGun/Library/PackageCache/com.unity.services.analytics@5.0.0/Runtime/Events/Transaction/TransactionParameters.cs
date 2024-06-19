using System;

namespace Unity.Services.Analytics
{
    public struct TransactionParameters
    {
        /// <summary>
        /// (Optional) The country where the transaction is taking place.
        /// If this is left null or empty, the machine's locale will be used.
        /// </summary>
        public string PaymentCountry;

        /// <summary>
        /// (Optional) The product identifier (known as a SKU) found in the store
        /// </summary>
        public string ProductID;

        /// <summary>
        /// (Optional) Parameter used by the IAP validation service
        /// </summary>
        public Int64? RevenueValidated;

        /// <summary>
        /// (Optional) An ID that uniquely identifies the transaction
        /// </summary>
        public string TransactionID;

        /// <summary>
        /// (Optional) Transaction receipt data as provided by the IAP store, used for validation
        /// </summary>
        public string TransactionReceipt;

        /// <summary>
        /// (Optional) The receipt signature from a Google store IAP
        /// </summary>
        public string TransactionReceiptSignature;

        /// <summary>
        /// (Optional) The server to use for receipt verification, if applicable
        /// </summary>
        public TransactionServer? TransactionServer;

        /// <summary>
        /// (Optional) The ID of the person or entity who is being transacted with
        /// </summary>
        public string TransactorID;

        /// <summary>
        /// (Optional) A unique ID for the SKU, linked to the store SKU ID
        /// </summary>
        public string StoreItemSkuID;

        /// <summary>
        /// (Optional) A unique ID for the purchased item
        /// </summary>
        public string StoreItemID;

        /// <summary>
        /// (Optional) The store where the transaction is taking place
        /// </summary>
        public string StoreID;

        /// <summary>
        /// (Optional) Identifies the source of the IAP
        /// </summary>
        public string StoreSourceID;

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
    }
}
