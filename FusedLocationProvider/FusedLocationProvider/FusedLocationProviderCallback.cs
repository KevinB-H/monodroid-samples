using System.Linq;

using Android.Gms.Location;

namespace FusedLocationProvider
{
    public class FusedLocationProviderCallback : LocationCallback
    {
        private readonly MainActivity activity;

        public FusedLocationProviderCallback(MainActivity activity)
        {
            this.activity = activity;
        }

        public override void OnLocationResult(LocationResult result)
        {
            if (result.Locations.Any())
            {
                var location = result.Locations.First();
                activity.latitude2.Text = activity.Resources.GetString(Resource.String.latitude_string, location.Latitude);
                activity.longitude2.Text = activity.Resources.GetString(Resource.String.longitude_string, location.Longitude);
                activity.provider2.Text = activity.Resources.GetString(Resource.String.requesting_updates_provider_string, location.Provider);
            }
            else
            {
                activity.latitude2.SetText(Resource.String.location_unavailable);
                activity.longitude2.SetText(Resource.String.location_unavailable);
                activity.provider2.SetText(Resource.String.could_not_get_last_location);
                activity.requestLocationUpdatesButton.SetText(Resource.String.request_location_button_text);
            }
        }
    }
}