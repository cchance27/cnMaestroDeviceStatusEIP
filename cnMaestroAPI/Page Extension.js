//Create the page extension to poll the service in EngageIP, on main owner or on specific owner doesn't matter

//Name: cnMaestro Canopy Status
//Page: overview/index

var cnDevices = [].slice.call(document.getElementsByClassName('noticeicon'));
cnDevices.forEach(function (item) {
  var cnItemValue = item.onmouseover.toString();

  if (cnItemValue.includes('0a-00-3e')) {
    var cnregex = /(?:[0-9a-fA-F]{2}-){5}[0-9a-fA-F]{2}/;
    var cnMac = cnItemValue.match(cnregex)[0];
   item.insertAdjacentHTML("afterend", cnStatusIcon);
  }
});