//Create the page extension to poll the service in EngageIP, on main owner or on specific owner doesn't matter

//Name: cnMaestro Canopy Status
//Page: overview/index

var cnDevices = [].slice.call(document.getElementsByClassName('noticeicon'));
cnDevices.forEach(function (item) {
  var cnItemValue = item.onmouseover.toString();

  if (cnItemValue.includes('0a-00-3e')) {
    var cnregex = /(?:[0-9a-fA-F]{2}-){5}[0-9a-fA-F]{2}/;
    var cnMac = cnItemValue.match(cnregex)[0];
    console.log("Performing Canopy Lookup: " + cnMac);
    new Ajax.Request(cnMac + '.cnmaestro', {
      asynchronous: true,
      method: 'get',
      onComplete: function onComplete(e) {
        var cnData = JSON.parse(e.responseText).data[0];

        if (cnData != null) {
          //console.log(JSON.stringify(cnData, null, 2));
          if (cnData != '') {
			// Format the timespan nice.
            var cnT = [Math.floor(cnData.status_time / 60 / 60 / 24), // DAYS
            Math.floor(cnData.status_time / 60 / 60) % 24, // HOURS
            Math.floor(cnData.status_time / 60) % 60, // MINUTES
            cnData.status_time % 60 // SECONDS
            ];
            var timeString = cnT[0] + " Days " + cnT[1] + ":" + cnT[2] + ":" + cnT[3];

            var cnStatusIcon = "";
            if (cnData.status === 'online') {
            	cnStatusIcon = " <a href=\"http://" + cnData.ip + "\" target=\"_blank\" /><img src=\"../images/check.gif\" class=\"noticeicon\" onmouseover=\"overlib('Connected To: " + cnData.tower + "<br/>IP: " + cnData.ip + "<br/>Device Type: " + cnData.product + "<br/>Firmware: " + cnData.software_version + "<br/>Status: " + cnData.status + " (" + timeString + ")',VAUTO,HAUTO)\" onmouseout=\"return nd();\" alt=\"Notice\"></a>";
            } else {
            	cnStatusIcon = " <img src=\"../images/error.gif\" class=\"noticeicon\" onmouseover=\"overlib('Connected To: " + cnData.tower + "<br/>IP: " + cnData.ip + "<br/>Device Type: " + cnData.product + "<br/>Firmware: " + cnData.software_version + "<br/>Status: " + cnData.status + " (" + timeString + ")',VAUTO,HAUTO)\" onmouseout=\"return nd();\" alt=\"Notice\">";
            }

            item.insertAdjacentHTML("afterend", cnStatusIcon);
          }
        }
      }
    });
  }
});