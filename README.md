# SystemMonitor C#
Simple app that runs in the notification area that periodically checks network type and display topology.

  *When Windows reconnects to a network after loosing connectivity to its DNS server the network is sometimes defaulted to "Public" 
  even if the network was previously set to "Private". When monitoring uptime for the device using ICMP Echo Requests this presents      
  a false positive as the firewall does not respond to the requests when set to "Public".
  
  *The app sets at startup, after 10 mins, and then every 30 mins afterward the network type to "Private" thru the associated 
  registry key.
  
  *On systems running multiple monitors that must all be set to "Clone" mode at all times problems may arise if one of the displays
  is removed from the system. Upon restarting the system or reconnection of the device the display topology may default to 
  "Extended" rendering one of the display devices unusable.
  
  *The app checks at startup, after 10 mins, and then every 30 mins for the number of displays connected; if > 1 it queries the OS 
  (using the WIN32 api) for the current display topology. If set to "Extended" it is set back to "Clone".
  
