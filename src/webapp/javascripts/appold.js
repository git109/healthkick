(function() {
  /*
  * Skeleton V1.0.3
  * Copyright 2011, Dave Gamache
  * www.getskeleton.com
  * Free to use under the MIT license.
  * http://www.opensource.org/licenses/mit-license.php
  * 7/17/2011
  */  $(document).ready(function() {
    var tabs;
    tabs = $("ul.tabs");
    return tabs.each(function(i) {
      var tab;
      tab = $(this).find("> li > a");
      return tab.click(function(e) {
        var contentLocation;
        contentLocation = $(this).attr("href") + "Tab";
        if (contentLocation.charAt(0) === "#") {
          e.preventDefault();
          tab.removeClass("active");
          $(this).addClass("active");
          return $(contentLocation).show().addClass("active").siblings().hide().removeClass("active");
        }
      });
    });
  });
}).call(this);
