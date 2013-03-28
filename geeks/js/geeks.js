var app = angular.module("geeks", ["ui", "ngResource", "http-auth-interceptor"])
    .config(['$routeProvider', '$httpProvider', function($routeProvider, $httpProvider) {
        $routeProvider
            .when("/", { templateUrl: "/geeks/Content/Partials/index.html" })
            .when("/events", { templateUrl: "/geeks/Home/Events", controller: "EventsCtrl" })
            .when("/friends", { templateUrl: "/geeks/Home/Friends", controller: "FriendsCtrl" })
            .when("/register", { templateUrl: "/geeks/Content/Partials/register.html" })
            .when("/google", { templateUrl: "/geeks/Content/Partials/googlelogin.html" })
            .when("/googlecallback", { templateUrl: "/geeks/Account/ExternalLoginCallback" })
            .when("/about", { templateUrl: "/geeks/Home/About" })
            .otherwise({ redirectTo: "/" });

    }])
    .factory("listData", function($http) {
        return {
            get: function(url, args) {
                return $http.get(url + "?" + $.param(args), { cache: false });
            }
        };
    })
    .controller("LoginCtrl", function($scope, $window, $http, $location, authService) {
        $scope.registering = false;
        $scope.register = function() {
            $scope.registering = true;
        },
        $scope.submit = function() {
            var payload = $.param({ Name: $scope.name, EmailAddress: $scope.email, Password: $scope.password, ConfirmPassword: $scope.confirm });
            var config = {
                headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' }
            };
            $http.post($scope.registering ? 'Account/Register' : 'Account/Login', payload, config).success(function() {
                authService.loginConfirmed($scope.email);
            });
        };
        $scope.googleLogin = function() {
            $location.path("/google");
        };
        $scope.buttonText = function() {
            return $scope.registering ? "Register" : "Sign In";
        };
    })
    .controller("EventsCtrl", function($scope, listData) {
        //$scope.events = events.query();
    })
    .controller("FriendsCtrl", function($scope, $http, listData) {

        $scope.searchArgs = {
            pageIndex: 0,
            pageSize: 10,
            friendSearch: null,
            unratedFriends: false
        };

        $scope.search = function() {
            listData.get("/geeks/Home/FriendsData", $scope.searchArgs).success(function(data) {
                $scope.friends = data.Friends;
                $scope.numberOfPages = data.NumberOfPages;
                $scope.prevClass = data.PageIndex > 0 ? "btn" : "btn disabled";
                $scope.nextClass = data.PageIndex + 1 >= data.NumberOfPages ? "btn disabled" : "btn";
            });
        };
        $scope.next = function() {
            if ($scope.searchArgs.pageIndex + 1 >= $scope.numberOfPages)
                return;
            $scope.searchArgs.pageIndex += 1;
            $scope.search();
        };
        $scope.prev = function() {
            if ($scope.searchArgs.pageIndex <= 0)
                return;
            $scope.searchArgs.pageIndex -= 1;
            $scope.search();
        };
        $scope.deleteFriend = function() {
            $http.post('/geeks/Home/DeleteFriend/' + $scope.selectedFriend.UserId).success(function() {
                angular.element("#delete-warning").modal("hide");
                $scope.search();
            });
        };
        $scope.deleteAllFriends = function() {
            $http.post('/geeks/Home/DeleteAllFriends').success(function() {
                angular.element("#delete-all-warning").modal("hide");
                $scope.search();
            });
        };
        $scope.search();
    })
    .controller("GeeksCtrl", function($rootScope, $scope, $http, $location) {
        $scope.loggedInUser = "";
        $scope.tab = 'home-nav';
        $scope.logoff = function() {
            $http.post('Account/LogOff').success(function() {
                $scope.loggedInUser = null;
                $location.path("/");
            });
        };
        $scope.signIn = function() {
            $rootScope.$broadcast('event:auth-loginRequired', { registering: false });
        };
        $scope.register = function() {
            $rootScope.$broadcast('event:auth-loginRequired', { registering: true });
        };
        $scope.$on('event:auth-loginConfirmed', function(event, data) {
            $scope.loggedInUser = data;
        });
        $scope.loggedIn = function() {
            return $scope.loggedInUser != null && $scope.loggedInUser != "";
        };

        $scope.isTabSelected = function(tabId) {
            return $scope.tab == tabId ? "active" : "";
        };
    })
    .directive("deleteFriend", function() {
        return {
            restrict: "E",
            replace: true,
            template: "<button ng-click='confirmDelete()' class='btn'><i class='icon-trash'></i></button>",
            link: function(scope, el, atts) {
                scope.confirmDelete = function() {
                    scope.$parent.selectedFriend = scope.friend;
                    angular.element(atts.warning).modal("show");
                };
            }
        };
    })
    .directive("truncate", function() {
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
    }).directive("login", function($location) {
        return {
            restrict: "E",
            replace: true,
            controller: "LoginCtrl",
            templateUrl: "Content/Partials/login.html",
            link: function(scope, elem) {
                scope.$on('event:auth-loginRequired', function(event, data) {
                    data = data || { registering: false };
                    scope.registering = data.registering;
                    scope.name = "";
                    scope.email = "";
                    scope.confirm = "";
                    scope.password = "";
                    elem.modal("show");
                });
                scope.$on('event:auth-loginConfirmed', function() {
                    elem.modal("hide");
                });
                scope.dismiss = function() {
                    elem.modal("hide");
                    $location.path("/");
                };
            }
        };
    });