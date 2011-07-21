(function() {
  /*
  * Skeleton V1.0.3
  * Copyright 2011, Dave Gamache
  * www.getskeleton.com
  * Free to use under the MIT license.
  * http://www.opensource.org/licenses/mit-license.php
  * 7/17/2011
  */  $(function() {
    var meal, meal2, tabs;
    tabs = $("ul.tabs");
    tabs.each(function(i) {
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
    window.Food = Backbone.Model.extend({
      defaults: {
        photo: "http://images1.wikia.nocookie.net/__cb20110514151745/primeval/images/4/4e/Pet-cat-insurance-1-.jpg",
        title: "title",
        description: "description",
        fat: 0,
        protein: 0,
        carbs: 0,
        calories: 0,
        notes: 0
      },
      initialize: function() {},
      update: function() {
        return this.save();
      },
      clear: function() {
        this.destroy();
        return this.view.remove();
      }
    });
    window.FoodList = Backbone.Collection.extend({
      model: window.Food,
      localStorage: new Store("foods")
    });
    window.FoodItems = new FoodList;
    window.FoodView = Backbone.View.extend({
      tagName: "li",
      template: _.template($('#item-template').html()),
      initialize: function() {
        _.bindAll(this, 'render');
        this.model.bind('change', this.render);
        return this.model.view = this;
      },
      render: function() {
        $(this.el).html(this.template(this.model.toJSON()));
        this.setContent();
        return this;
      },
      setContent: function() {
        var title;
        title = this.model.get('title');
        return this.$('.title').text(title);
      }
    });
    window.AppView = Backbone.View.extend({
      el: $("#healthkickapp"),
      events: {
        "click #butt": "addAll"
      },
      initialize: function() {
        _.bindAll(this, 'addOne', 'addAll', 'render');
        FoodItems.bind('add', this.addOne);
        FoodItems.bind('reset', this.addAll);
        return FoodItems.bind('all', this.render);
      },
      addOne: function(food) {
        var view;
        view = new FoodView({
          model: food
        });
        return this.$("#feed").append(view.render().el);
      },
      addAll: function() {
        return window.FoodItems.each(this.addOne);
      }
    });
    window.App = new AppView;
    meal = new Food({
      title: 'a',
      description: 'b'
    });
    meal2 = new Food({
      title: 'c',
      description: 'd'
    });
    window.App.addOne(meal);
    return window.App.addOne(meal2);
  });
}).call(this);
