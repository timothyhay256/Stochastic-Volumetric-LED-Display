namespace Unity.Services.Analytics
{
    public class AcquisitionSourceParameters
    {
        /// <summary>
        /// (Required) The name of the specific marketing provider used to drive traffic to the game.
        /// This should be a short identifiable string as this will be the name displayed when filtering or grouping by an acquisition channel.
        /// </summary>
        public string Channel;

        /// <summary>
        /// (Required) The ID of the acquisition campaign
        /// </summary>
        public string CampaignId;

        /// <summary>
        /// (Required) The acquisition campaign creative ID
        /// </summary>
        public string CreativeId;

        /// <summary>
        /// (Required) The acquisition campaign name e.g. Interstitial:Halloween21
        /// </summary>
        public string CampaignName;

        /// <summary>
        /// (Required) The name of the attribution provider in use e.g. Adjust, AppsFlyer, Singular
        /// </summary>
        public string Provider;

        /// <summary>
        /// (Optional) The cost of the install e.g. 2.36
        /// </summary>
        public float? Cost;

        /// <summary>
        /// (Optional) The install cost currency e.g. USD
        /// </summary>
        public string CostCurrency;

        /// <summary>
        /// (Optional) The acquisition campaign network e.g. Ironsource, Facebook Ads
        /// </summary>
        public string Network;

        /// <summary>
        /// (Optional) The acquisition campaign type. e.g. CPI
        /// </summary>
        public string CampaignType;
    }
}
