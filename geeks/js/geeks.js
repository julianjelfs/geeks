var app = angular.module("geeks", ["ui"])
    .config(['$routeProvider', '$locationProvider', function($routeProvider, $locationProvider) {
        $routeProvider
            .when("/", { templateUrl: "/geeks/Content/Partials/index.html" })
            .when("/events", { templateUrl: "/geeks/Content/Partials/events.html" })
            .otherwise({ redirectTo: "/" });
        
        //$locationProvider.html5Mode(true).hashPrefix('!');
    }]);