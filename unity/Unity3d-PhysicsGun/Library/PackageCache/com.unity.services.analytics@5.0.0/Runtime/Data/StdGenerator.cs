using System;
using System.Runtime.CompilerServices;
using Unity.Services.Analytics.Internal;

[assembly: InternalsVisibleTo("Unity.Services.Analytics.Tests")]

namespace Unity.Services.Analytics.Data
{
    interface IDataGenerator
    {
        void SetBuffer(IBuffer buffer);

        void GameRunning(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier);
        void SdkStartup(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier);
        void NewPlayer(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier, string deviceModel);
        void GameStarted(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier, string idLocalProject, string osVersion, bool isTiny, bool debugDevice, string userLocale);
        void GameEnded(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier, DataGenerator.SessionEndState quitState);
        void AdImpression(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier,
            AdImpressionParameters adImpressionParameters);
        void Transaction(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier, TransactionParameters transactionParameters);
        void TransactionFailed(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier, TransactionFailedParameters transactionParameters);
        void ClientDevice(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier,
            string cpuType, string gpuType, Int64 cpuCores, Int64 ramTotal, Int64 screenWidth, Int64 screenHeight, Int64 screenDPI);
        void AcquisitionSource(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier, AcquisitionSourceParameters acquisitionSourceParameters);
    }

    /// <summary>
    /// DataGenerator is used to push event data into the internal buffer.
    /// The reason its split like this is so we can test the output from
    /// The DataGenerator + InternalBuffer. If this output is validated we
    /// can be pretty confident we are always producing valid JSON for the
    /// backend.
    /// </summary>
    class DataGenerator : IDataGenerator
    {
        IBuffer m_Buffer;

        public void SetBuffer(IBuffer buffer)
        {
            m_Buffer = buffer;
        }

        public void SdkStartup(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier)
        {
            m_Buffer.PushStartEvent("sdkStart", datetime, 1, true);
            m_Buffer.PushString(SdkVersion.SDK_VERSION, "sdkVersion");

            // Event Params
            commonParams.SerializeCommonEventParams(ref m_Buffer, callingMethodIdentifier);
            m_Buffer.PushString("com.unity.services.analytics", "sdkName"); // Schema: Required

            m_Buffer.PushEndEvent();
        }

        public void GameRunning(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier)
        {
            m_Buffer.PushStartEvent("gameRunning", datetime, 1, true);

            // Event Params
            commonParams.SerializeCommonEventParams(ref m_Buffer, callingMethodIdentifier);

            m_Buffer.PushEndEvent();
        }

        public void NewPlayer(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier, string deviceModel)
        {
            m_Buffer.PushStartEvent("newPlayer", datetime, 1, true);

            // Event Params
            commonParams.SerializeCommonEventParams(ref m_Buffer, callingMethodIdentifier);
            // We aren't sending deviceBrand at the moment as deviceModel is sufficient.
            // UA1 did not send deviceBrand either. See JIRA-196 for more info.
            m_Buffer.PushString(deviceModel, "deviceModel"); // Schema: Optional

            m_Buffer.PushEndEvent();
        }

        public void GameStarted(DateTime datetime, StdCommonParams commonParams,
            string callingMethodIdentifier, string idLocalProject, string osVersion, bool isTiny, bool debugDevice, string userLocale)
        {
            m_Buffer.PushStartEvent("gameStarted", datetime, 1, true);

            // Event Params
            commonParams.SerializeCommonEventParams(ref m_Buffer, callingMethodIdentifier);

            // Schema: Required
            m_Buffer.PushString(userLocale, "userLocale");

            // Schema: Optional
            if (!String.IsNullOrEmpty(idLocalProject))
            {
                m_Buffer.PushString(idLocalProject, "idLocalProject");
            }
            m_Buffer.PushString(osVersion, "osVersion");
            m_Buffer.PushBool(isTiny, "isTiny");
            m_Buffer.PushBool(debugDevice, "debugDevice");

            m_Buffer.PushEndEvent();
        }

        // Keep the enum values in Caps!
        // We stringify the values.
        // These values aren't listed as an enum the Schema, but they are listed
        // values here http://go/UA2_Spreadsheet
        internal enum SessionEndState
        {
            PAUSED,
            KILLEDINBACKGROUND,
            KILLEDINFOREGROUND,
            QUIT,
        }

        public void GameEnded(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier, SessionEndState quitState)
        {
            m_Buffer.PushStartEvent("gameEnded", datetime, 1, true);

            // Event Params
            commonParams.SerializeCommonEventParams(ref m_Buffer, callingMethodIdentifier);

            m_Buffer.PushString(quitState.ToString(), "sessionEndState"); // Schema: Required

            m_Buffer.PushEndEvent();
        }

        public void AdImpression(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier, AdImpressionParameters adImpressionParameters)
        {
            m_Buffer.PushStartEvent("adImpression", datetime, 1, true);

            // Event Params
            commonParams.SerializeCommonEventParams(ref m_Buffer, callingMethodIdentifier);

            // Schema: Required

            m_Buffer.PushString(adImpressionParameters.AdCompletionStatus.ToString().ToUpperInvariant(), "adCompletionStatus");
            m_Buffer.PushString(adImpressionParameters.AdProvider.ToString().ToUpperInvariant(), "adProvider");
            m_Buffer.PushString(adImpressionParameters.PlacementID, "placementId");
            m_Buffer.PushString(adImpressionParameters.PlacementName, "placementName");

            // Schema: Optional

            if (adImpressionParameters.AdEcpmUsd is double adEcpmUsdValue)
            {
                m_Buffer.PushDouble(adEcpmUsdValue, "adEcpmUsd");
            }

            if (adImpressionParameters.PlacementType != null)
            {
                m_Buffer.PushString(adImpressionParameters.PlacementType.ToString(), "placementType");
            }

            if (!string.IsNullOrEmpty(adImpressionParameters.SdkVersion))
            {
                m_Buffer.PushString(adImpressionParameters.SdkVersion, "adSdkVersion");
            }

            if (!string.IsNullOrEmpty(adImpressionParameters.AdImpressionID))
            {
                m_Buffer.PushString(adImpressionParameters.AdImpressionID, "adImpressionID");
            }

            if (!string.IsNullOrEmpty(adImpressionParameters.AdStoreDstID))
            {
                m_Buffer.PushString(adImpressionParameters.AdStoreDstID, "adStoreDestinationID");
            }

            if (!string.IsNullOrEmpty(adImpressionParameters.AdMediaType))
            {
                m_Buffer.PushString(adImpressionParameters.AdMediaType, "adMediaType");
            }

            if (adImpressionParameters.AdTimeWatchedMs is Int64 adTimeWatchedMsValue)
            {
                m_Buffer.PushInt64(adTimeWatchedMsValue, "adTimeWatchedMs");
            }

            if (adImpressionParameters.AdTimeCloseButtonShownMs is Int64 adTimeCloseButtonShownMsValue)
            {
                m_Buffer.PushInt64(adTimeCloseButtonShownMsValue, "adTimeCloseButtonShownMs");
            }

            if (adImpressionParameters.AdLengthMs is Int64 adLengthMsValue)
            {
                m_Buffer.PushInt64(adLengthMsValue, "adLengthMs");
            }

            if (adImpressionParameters.AdHasClicked is bool adHasClickedValue)
            {
                m_Buffer.PushBool(adHasClickedValue, "adHasClicked");
            }

            if (!string.IsNullOrEmpty(adImpressionParameters.AdSource))
            {
                m_Buffer.PushString(adImpressionParameters.AdSource, "adSource");
            }

            if (!string.IsNullOrEmpty(adImpressionParameters.AdStatusCallback))
            {
                m_Buffer.PushString(adImpressionParameters.AdStatusCallback, "adStatusCallback");
            }

            m_Buffer.PushEndEvent();
        }

        public void AcquisitionSource(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier, AcquisitionSourceParameters acquisitionSourceParameters)
        {
            m_Buffer.PushStartEvent("acquisitionSource", datetime, 1, true);

            // Event Params
            commonParams.SerializeCommonEventParams(ref m_Buffer, callingMethodIdentifier);

            //other event parameters
            // Required
            m_Buffer.PushString(acquisitionSourceParameters.Channel, "acquisitionChannel");
            m_Buffer.PushString(acquisitionSourceParameters.CampaignId, "acquisitionCampaignId");
            m_Buffer.PushString(acquisitionSourceParameters.CreativeId, "acquisitionCreativeId");
            m_Buffer.PushString(acquisitionSourceParameters.CampaignName, "acquisitionCampaignName");
            m_Buffer.PushString(acquisitionSourceParameters.Provider, "acquisitionProvider");

            if (!string.IsNullOrEmpty(acquisitionSourceParameters.CampaignType))
            {
                m_Buffer.PushString(acquisitionSourceParameters.CampaignType, "acquisitionCampaignType");
            }

            if (!string.IsNullOrEmpty(acquisitionSourceParameters.Network))
            {
                m_Buffer.PushString(acquisitionSourceParameters.Network, "acquisitionNetwork");
            }

            if (!string.IsNullOrEmpty(acquisitionSourceParameters.CostCurrency))
            {
                m_Buffer.PushString(acquisitionSourceParameters.CostCurrency, "acquisitionCostCurrency");
            }

            if (acquisitionSourceParameters.Cost is float cost)
            {
                m_Buffer.PushFloat(cost, "acquisitionCost");
            }

            m_Buffer.PushEndEvent();
        }

        public void Transaction(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier, TransactionParameters transactionParameters)
        {
            m_Buffer.PushStartEvent("transaction", datetime, 1, true);
            // Event Params
            commonParams.SerializeCommonEventParams(ref m_Buffer, callingMethodIdentifier);

            if (!string.IsNullOrEmpty(SdkVersion.SDK_VERSION))
            {
                m_Buffer.PushString(SdkVersion.SDK_VERSION, "sdkVersion");
            }

            if (!string.IsNullOrEmpty(transactionParameters.PaymentCountry))
            {
                m_Buffer.PushString(transactionParameters.PaymentCountry, "paymentCountry");
            }

            if (!string.IsNullOrEmpty(transactionParameters.ProductID))
            {
                m_Buffer.PushString(transactionParameters.ProductID, "productID");
            }

            if (transactionParameters.RevenueValidated.HasValue)
            {
                m_Buffer.PushInt64(transactionParameters.RevenueValidated.Value, "revenueValidated");
            }

            if (!string.IsNullOrEmpty(transactionParameters.TransactionID))
            {
                m_Buffer.PushString(transactionParameters.TransactionID, "transactionID");
            }

            if (!string.IsNullOrEmpty(transactionParameters.TransactionReceipt))
            {
                m_Buffer.PushString(transactionParameters.TransactionReceipt, "transactionReceipt");
            }

            if (!string.IsNullOrEmpty(transactionParameters.TransactionReceiptSignature))
            {
                m_Buffer.PushString(transactionParameters.TransactionReceiptSignature, "transactionReceiptSignature");
            }

            if (!string.IsNullOrEmpty(transactionParameters.TransactionServer?.ToString()))
            {
                m_Buffer.PushString(transactionParameters.TransactionServer.ToString(), "transactionServer");
            }

            if (!string.IsNullOrEmpty(transactionParameters.TransactorID))
            {
                m_Buffer.PushString(transactionParameters.TransactorID, "transactorID");
            }

            if (!string.IsNullOrEmpty(transactionParameters.StoreItemSkuID))
            {
                m_Buffer.PushString(transactionParameters.StoreItemSkuID, "storeItemSkuID");
            }

            if (!string.IsNullOrEmpty(transactionParameters.StoreItemID))
            {
                m_Buffer.PushString(transactionParameters.StoreItemID, "storeItemID");
            }

            if (!string.IsNullOrEmpty(transactionParameters.StoreID))
            {
                m_Buffer.PushString(transactionParameters.StoreID, "storeID");
            }

            if (!string.IsNullOrEmpty(transactionParameters.StoreSourceID))
            {
                m_Buffer.PushString(transactionParameters.StoreSourceID, "storeSourceID");
            }

            // Required
            m_Buffer.PushString(transactionParameters.TransactionName, "transactionName");
            m_Buffer.PushString(transactionParameters.TransactionType.ToString(), "transactionType");
            SetProduct("productsReceived", transactionParameters.ProductsReceived);
            SetProduct("productsSpent", transactionParameters.ProductsSpent);

            m_Buffer.PushEndEvent();
        }

        public void TransactionFailed(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier, TransactionFailedParameters parameters)
        {
            m_Buffer.PushStartEvent("transactionFailed", datetime, 1, true);
            // Event Params
            commonParams.SerializeCommonEventParams(ref m_Buffer, callingMethodIdentifier);

            if (!string.IsNullOrEmpty(SdkVersion.SDK_VERSION))
            {
                m_Buffer.PushString(SdkVersion.SDK_VERSION, "sdkVersion");
            }

            if (!string.IsNullOrEmpty(parameters.PaymentCountry))
            {
                m_Buffer.PushString(parameters.PaymentCountry, "paymentCountry");
            }

            if (!string.IsNullOrEmpty(parameters.ProductID))
            {
                m_Buffer.PushString(parameters.ProductID, "productID");
            }

            if (parameters.RevenueValidated.HasValue)
            {
                m_Buffer.PushInt64(parameters.RevenueValidated.Value, "revenueValidated");
            }

            if (!string.IsNullOrEmpty(parameters.TransactionID))
            {
                m_Buffer.PushString(parameters.TransactionID, "transactionID");
            }

            if (!string.IsNullOrEmpty(parameters.TransactionServer?.ToString()))
            {
                m_Buffer.PushString(parameters.TransactionServer.ToString(), "transactionServer");
            }

            if (parameters.EngagementID != null)
            {
                m_Buffer.PushInt64((long)parameters.EngagementID, "engagementID");
            }

            if (!string.IsNullOrEmpty(parameters.GameStoreID))
            {
                m_Buffer.PushString(parameters.GameStoreID, "gameStoreID");
            }

            if (!string.IsNullOrEmpty(parameters.AmazonUserID))
            {
                m_Buffer.PushString(parameters.AmazonUserID, "amazonUserID");
            }

            if (parameters.IsInitiator != null)
            {
                m_Buffer.PushBool((bool)parameters.IsInitiator, "isInitiator");
            }

            if (!string.IsNullOrEmpty(parameters.StoreItemSkuID))
            {
                m_Buffer.PushString(parameters.StoreItemSkuID, "storeItemSkuID");
            }

            if (!string.IsNullOrEmpty(parameters.StoreItemID))
            {
                m_Buffer.PushString(parameters.StoreItemID, "storeItemID");
            }

            if (!string.IsNullOrEmpty(parameters.StoreID))
            {
                m_Buffer.PushString(parameters.StoreID, "storeID");
            }

            if (!string.IsNullOrEmpty(parameters.StoreSourceID))
            {
                m_Buffer.PushString(parameters.StoreSourceID, "storeSourceID");
            }

            // Required
            m_Buffer.PushString(parameters.TransactionName, "transactionName");
            m_Buffer.PushString(parameters.TransactionType.ToString(), "transactionType");
            SetProduct("productsReceived", parameters.ProductsReceived);
            SetProduct("productsSpent", parameters.ProductsSpent);

            m_Buffer.PushString(parameters.FailureReason, "failureReason");

            m_Buffer.PushEndEvent();
        }

        public void ClientDevice(DateTime datetime, StdCommonParams commonParams, string callingMethodIdentifier,
            string cpuType, string gpuType, Int64 cpuCores, Int64 ramTotal, Int64 screenWidth, Int64 screenHeight, Int64 screenDPI)
        {
            m_Buffer.PushStartEvent("clientDevice", datetime, 1, true);

            commonParams.SerializeCommonEventParams(ref m_Buffer, callingMethodIdentifier);

            // Schema: Optional
            m_Buffer.PushString(cpuType, "cpuType");
            m_Buffer.PushString(gpuType, "gpuType");
            m_Buffer.PushInt64(cpuCores, "cpuCores");
            m_Buffer.PushInt64(ramTotal, "ramTotal");
            m_Buffer.PushInt64(screenWidth, "screenWidth");
            m_Buffer.PushInt64(screenHeight, "screenHeight");
            m_Buffer.PushInt64(screenDPI, "screenResolution");

            m_Buffer.PushEndEvent();
        }

        void SetProduct(string productName, Product product)
        {
            m_Buffer.PushObjectStart(productName);

            if (product.RealCurrency.HasValue)
            {
                m_Buffer.PushObjectStart("realCurrency");
                m_Buffer.PushString(product.RealCurrency.Value.RealCurrencyType, "realCurrencyType");
                m_Buffer.PushInt64(product.RealCurrency.Value.RealCurrencyAmount, "realCurrencyAmount");
                m_Buffer.PushObjectEnd();
            }

            if (product.VirtualCurrencies != null && product.VirtualCurrencies.Count != 0)
            {
                m_Buffer.PushArrayStart("virtualCurrencies");
                foreach (var virtualCurrency in product.VirtualCurrencies)
                {
                    m_Buffer.PushObjectStart();
                    m_Buffer.PushObjectStart("virtualCurrency");
                    m_Buffer.PushString(virtualCurrency.VirtualCurrencyName, "virtualCurrencyName");
                    m_Buffer.PushString(virtualCurrency.VirtualCurrencyType.ToString(), "virtualCurrencyType");
                    m_Buffer.PushInt64(virtualCurrency.VirtualCurrencyAmount, "virtualCurrencyAmount");
                    m_Buffer.PushObjectEnd();
                    m_Buffer.PushObjectEnd();
                }
                m_Buffer.PushArrayEnd();
            }

            if (product.Items != null && product.Items.Count != 0)
            {
                m_Buffer.PushArrayStart("items");
                foreach (var item in product.Items)
                {
                    m_Buffer.PushObjectStart();
                    m_Buffer.PushObjectStart("item");
                    m_Buffer.PushString(item.ItemName, "itemName");
                    m_Buffer.PushString(item.ItemType, "itemType");
                    m_Buffer.PushInt64(item.ItemAmount, "itemAmount");
                    m_Buffer.PushObjectEnd();
                    m_Buffer.PushObjectEnd();
                }
                m_Buffer.PushArrayEnd();
            }

            m_Buffer.PushObjectEnd();
        }
    }
}
