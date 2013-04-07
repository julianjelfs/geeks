﻿var app = angular.module('geeks', ['ui']);

app.factory("listData", function($http) {
    return {
        get: function(url, args) {
            return $http.get(url + "?" + $.param(args), { cache: false });
        }
    };
}).factory("xsrfPost", function($http) {
    return {
        post: function(url, args) {
            var config = {
                headers: { '__RequestVerificationToken': angular.element("input[name='__RequestVerificationToken']").val() }
            };
            return $http.post(url, args, config);
        }
    };
}).directive("friendPicker", function($http) {
    return {
        restrict : "E",
        replace : true,
        template : "<div><input class='typeahead' type='text' placeholder='Type here to find friends to add' data-provide='typeahead' autocomplete='false' style='width: 98%' />"
                    +    "<div id='invitees' class='well well-small'>"
                    +        "<div ng-repeat='invitee in invitees' class='alert alert-info'>"
                    +            "<button ng-click='remove(invitee.PersonId)' type='button' class='close' data-dismiss='alert'>&times;</button>"
                    +            "{{invitee.EmailAddress}}"
                    +            "<rate-friend model='invitee'></rate-friend>"
                    +        "</div>"
                    +        "<span ng-show='unratedFriends()'>Make sure you rate all friends that you invite for the best result</span>"
                    +    "</div></div>",
        link : function(scope, el, atts) {
            var map = {};
            scope.invitees = [];
            angular.element("input.typeahead", el).typeahead({
                updater: function(item) {
                    scope.$apply(function() {
                        scope.add({
                            PersonId: map[item].PersonId,
                            EmailAddress: item,
                            Rating: map[item].Rating,
                            EmailSent: false
                        });
                    });
                    return "";
                },
                source: function(query, process) {
                    return $http.get('/geeks/Home/LookupFriends/' + query).success(function(data) {
                        var emails = [];
                        map = data;
                        for (var prop in data) {
                            emails.push(prop);
                        }
                        return process(emails);
                    });
                }
            });    
            scope.add = function(obj) {
                scope.invitees.push({
                    EmailAddress: obj.EmailAddress,
                    PersonId: obj.PersonId,
                    Rating: obj.Rating,
                    EmailSent: obj.EmailSent
                });
            };
            scope.remove = function(personId) {
                scope.invitees = $.grep(scope.invitees, function(item) {
                    return item.personId != personId;
                });
            };
            scope.unratedFriends = function() {
                return $.grep(scope.invitees, function(item) {
                    return item.Rating == 0;
                }).length > 0;
            };
        }
    }
}).directive("rateFriend", function($compile, xsrfPost) {
    var regex = new RegExp("{x}", "g");
    var rankHtml = "<div>";
    var rankButton = "<span ng-click='rateFriend({x})' ng-class='ratingClass({x})'>{x}</span>";
    for (var i = 0; i < 10; i++) {
        rankHtml += rankButton.replace(regex,i+1);
    }
    rankHtml += "</div>";
    console.log("created popover template");
    var popover;

    return {
        restrict: "E",
        replace: true,
        scope : {
            model : "=model"
        },
        template: "<span title='Rate your friend' class='rating pull-right'><span ng-class='ratingClass(model.Rating)'>{{model.Rating}}</span></span>",
        link: function(scope, el, atts) {
            console.log("linking add friend");
            el.popover({
                content: $compile(rankHtml)(scope),
                placement: "top",
                trigger: "manual",
                html: true,
                title: ''
            }).click(function(){
                if(popover != null && popover != el)
                    popover.popover("hide");
                el.popover("show");
                popover = el;
            });

            scope.ratingClass = function(index) {
                var cls = "rank badge";
                if(index <= scope.model.Rating
                    && scope.model.Rating > 0)
                    cls += " badge-warning";
                return cls;
            }

            scope.rateFriend = function(rating) {
                console.log("Rating friend " + scope.model.Name + " as " + rating);
                scope.model.Rating = rating;
                xsrfPost.post('/geeks/Home/RateFriend', {
                    id : scope.model.PersonId,
                    rating : rating
                }).success(function() {
                    el.popover("hide");
                });
            };
        }
    };
}).directive("truncate", function() {
    return {
        priority: -1,
        link: function(scope, el, atts) {
            scope.$watch(atts.ngBind, function(value) {
                var len = parseInt(atts.truncate);
                el.text(value == undefined
                    ? '' : (value.length > len
                        ? value.substring(0, len) + '...' : value));
            });
        }
    };
});