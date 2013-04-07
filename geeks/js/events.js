var app = angular.module("geeks");
app.controller("EventsCtrl", function($scope, $http, listData, xsrfPost) {
    $scope.searchArgs = {
        pageIndex: 0,
        pageSize: 10,
        search: null
    };
        
    $scope.search = function() {
        listData.get("/geeks/home/EventsData", $scope.searchArgs).success(function(data) {
            $scope.events = data.Events;
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
        xsrfPost.post('/geeks/Home/DeleteEvent/' + $scope.selectedEvent.Id).success(function() {
            angular.element("#delete-warning").modal("hide");
            $scope.search();
        });
    };
    $scope.search();
}).directive("deleteEvent", function() {
    return {
        restrict: "E",
        replace: true,
        template: "<button ng-click='confirmDelete()' class='btn'><i class='icon-trash'></i></button>",
        link: function(scope, el, atts) {
            scope.confirmDelete = function() {
                scope.$parent.selectedEvent = scope.ev;
                angular.element(atts.warning).modal("show");
            };
        }
    };
});

$(function() {
    $("#nav-bar li.active").removeClass("active");
    $("#events-nav").addClass("active");
});