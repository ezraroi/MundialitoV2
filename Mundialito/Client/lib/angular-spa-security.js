angular.module('security', [])
	.constant('security.urls', {
		site: '/',
		manage: '/manage',
		join: '/api/account/register',
		login: '/api/account/login',
		googleLogin: '/api/account/signin-google',
		logout: '/api/account/logout',
		forgotPassword: '/api/account/forgot',
		resetPassword: '/api/account/reset',
		confirmEmail: '/api/account/confirmEmail',
		userInfo: '/api/account/userInfo',
		changePassword: '/api/account/changePassword',
		// externalLogins: '/',
		// manageInfo: '/api/account/manageInfo',
		// registerExternal: '/api/account/registerExternal',
		// addExternalLogin: '/api/account/addExternalLogin',
		// removeLogin: '/api/account/removeLogin'
	})
	.factory('security.api', ['$http', 'security.urls', function ($http, Urls) {
		//Parameterize - Necessary for funky login expectations...
		var formdataHeader = { 'Content-Type': 'application/x-www-form-urlencoded' };
		var parameterize = function (data) {
			var param = function (obj) {
				var query = '';
				var subValue, fullSubName, innerObj, i;
				angular.forEach(obj, function (value, name) {
					if (value instanceof Array) {
						for (i = 0; i < value.length; ++i) {
							subValue = value[i];
							fullSubName = name + '[' + i + ']';
							innerObj = {};
							innerObj[fullSubName] = subValue;
							query += param(innerObj) + '&';
						}
					}
					else if (value instanceof Object) {
						angular.forEach(value, function (subValue, subName) {
							fullSubName = name + '[' + subName + ']';
							innerObj = {};
							innerObj[fullSubName] = subValue;
							query += param(innerObj) + '&';
						});
					}
					else if (value !== undefined && value !== null) {
						query += encodeURIComponent(name) + '=' + encodeURIComponent(value) + '&';
					}
				});

				return query.length ? query.substr(0, query.length - 1) : query;
			};
			return angular.isObject(data) && String(data) !== '[object File]' ? param(data) : data;
		};

		var Api = {
			getUserInfo: (accessToken) => $http({ url: Urls.userInfo, method: 'GET', headers: { 'Authorization': 'Bearer ' + accessToken } }),
			login: (data) => $http({ method: 'POST', url: Urls.login, data: data }),
			googleLogin: (data) => $http({ method: 'POST', url: Urls.googleLogin, data: data }),
			logout: () => $http({ method: 'POST', url: Urls.logout }),
			register: (data) => $http({ method: 'POST', url: Urls.join, data: data }),
			forgotPassword: (data) => $http({ method: 'POST', url: Urls.forgotPassword, data: data }),
			resetPassword: (data) => $http({ method: 'POST', url: Urls.resetPassword, data: data }),
			confirmEmail: (data) => $http({ method: 'GET', url: Urls.confirmEmail + '?code=' + encodeURIComponent(data.code) + '&userId=' + encodeURIComponent(data.userId) }),
			changePassword: (data) => $http({ method: 'POST', url: Urls.changePassword, data: data }),
			manageInfo: () => $http({ method: 'GET', url: Urls.manageInfo + '?returnUrl=' + encodeURIComponent(Urls.site) + '&generateState=false' }),
			registerExternal: (accessToken, data) => $http({ method: 'POST', url: Urls.registerExternal, data: data, headers: { 'Authorization': 'Bearer ' + accessToken } }),
			addExternalLogin: (accessToken, externalAccessToken) => $http({ method: 'POST', url: Urls.addExternalLogin, data: { externalAccessToken: externalAccessToken }, headers: { 'Authorization': 'Bearer ' + accessToken } }),
			removeLogin: (data) => $http({ method: 'POST', url: Urls.removeLogin, data: data })
		};

		return Api;
	}])
	.provider('security', ['security.urls', function (Urls) {
		var securityProvider = this;
		//Options
		securityProvider.registerThenLogin = true;
		securityProvider.usePopups = false;
		securityProvider.urls = {
			login: '/login',
			registerExternal: '/registerExternal',
			postLogout: '/login',
			home: '/'
		};
		securityProvider.apiUrls = Urls;
		securityProvider.events = {
			login: null,
			logout: null,
			register: null,
			reloadUser: null,
			closeOAuthWindow: null
		};

		securityProvider.$get = ['security.api', '$q', '$http', '$location', '$timeout', function (Api, $q, $http, $location, $timeout) {
			//Private Variables
			var externalLoginWindowTimer = null;

			//Private Methods
			var parseQueryString = function (queryString) {
				var data = {},
					pairs, pair, separatorIndex, escapedKey, escapedValue, key, value;

				if (queryString === null) {
					return data;
				}

				pairs = queryString.split("&");

				for (var i = 0; i < pairs.length; i++) {
					pair = pairs[i];
					separatorIndex = pair.indexOf("=");

					if (separatorIndex === -1) {
						escapedKey = pair;
						escapedValue = null;
					} else {
						escapedKey = pair.substr(0, separatorIndex);
						escapedValue = pair.substr(separatorIndex + 1);
					}

					key = decodeURIComponent(escapedKey);
					value = decodeURIComponent(escapedValue);

					data[key] = value;
				}

				return data;
			};
			var accessToken = function (accessToken, persist) {
				if (accessToken) {
					if (accessToken == 'clear') {
						localStorage.removeItem('accessToken');
						sessionStorage.removeItem('accessToken');
						delete $http.defaults.headers.common.Authorization;
					} else {
						if (persist) localStorage.accessToken = accessToken;
						else sessionStorage.accessToken = accessToken;
						$http.defaults.headers.common.Authorization = 'Bearer ' + accessToken;
					}
				}
				return sessionStorage.accessToken || localStorage.accessToken;
			};
			var associating = function (newValue) {
				if (newValue == 'clear') {
					delete localStorage.associating;
					return;
				}
				if (newValue) localStorage.associating = newValue;
				return localStorage.associating;
			};
			var redirectTarget = function (newTarget) {
				if (newTarget == 'clear') {
					delete localStorage.redirectTarget;
					return;
				}
				if (newTarget) localStorage.redirectTarget = newTarget;
				return localStorage.redirectTarget;
			};

			var initialize = function () {
				//Check for access token and get user info
				if (accessToken()) {
					accessToken(accessToken());
					return Api.getUserInfo(accessToken()).then((user) => {
						Security.user = user.data;
						if (securityProvider.events.reloadUser) securityProvider.events.reloadUser(Security, user); // Your Register events
					});
				}
				return $q.when();
			};

			//Public Variables
			var Security = this;
			Security.user = null;
			Security.externalUser = null;
			Security.externalLogins = [];

			//Public Methods
			Security.login = (data) => {
				var deferred = $q.defer();
				data.grant_type = 'password';
				Api.login(data).then((res) => { return doLogin(res, data, deferred)}).catch((errorData) => {
					deferred.reject(errorData);
				});
				return deferred.promise;
			};

			Security.googleLogin = (data) => {
				var deferred = $q.defer();
				Api.googleLogin(data).then((res) => { return doLogin(res, { rememberMe : true}, deferred)}).catch((errorData) => {
					deferred.reject(errorData);
				});
				return deferred.promise;
			};

			Security.logout = () => {
				var deferred = $q.defer();

				Api.logout().then(() => {
					Security.user = null;
					accessToken('clear');
					redirectTarget('clear');
					if (securityProvider.events.logout) securityProvider.events.logout(Security); // Your Logout events
					$location.path(securityProvider.urls.postLogout);
					deferred.resolve();
				}).catch((errorData) => {
					accessToken('clear');
					deferred.reject(errorData);
				});

				return deferred.promise;
			};

			Security.register = function (data) {
				var deferred = $q.defer();

				Api.register(data).then(() => {
					if (securityProvider.events.register) securityProvider.events.register(Security); // Your Register events
					if (securityProvider.registerThenLogin) {
						Security.login(data).then((user) => {
							deferred.resolve(user.data);
						}, function (errorData) {
							deferred.reject(errorData);
						});
					} else {
						deferred.resolve();
					}
				}).catch((errorData) => {
					deferred.reject(errorData);
				});

				return deferred.promise;
			};

			Security.registerExternal = function () {
				var deferred = $q.defer();

				if (!Security.externalUser) {
					deferred.reject();
				} else {
					Api.registerExternal(Security.externalUser.access_token, Security.externalUser).then(() => {
						//Success
						deferred.resolve(Security.loginWithExternal(Security.externalUser.provider));
						Security.externalUser = null;
					}).catch((errorData) => {
						deferred.reject(errorData);
					});
				}

				return deferred.promise;
			};

			Security.forgotPassword = function (data) {
				var deferred = $q.defer();

				Api.forgotPassword(data).then((data) => {
					deferred.resolve(data.data);
				}).catch((errorData) => {
					deferred.reject(errorData);
				});

				return deferred.promise;
			};

			Security.resetPassword = function (data) {
				var deferred = $q.defer();

				Api.resetPassword(data).then(function (data) {
					deferred.resolve(data);
				}).catch(function (errorData) {
					deferred.reject(errorData);
				});

				return deferred.promise;
			};

			Security.confirmEmail = function (data) {
				var deferred = $q.defer();

				Api.confirmEmail(data).then((data) => {
					deferred.resolve(data.data);
				}).catch((errorData) => {
					deferred.reject(errorData);
				});

				return deferred.promise;
			};

			Security.mangeInfo = function () {
				var deferred = $q.defer();

				Api.manageInfo().then((manageInfo) => {
					deferred.resolve(manageInfo.data);
				}).catch((errorData) => {
					deferred.reject(errorData);
				});

				return deferred.promise;
			};

			Security.changePassword = (data) => {
				var deferred = $q.defer();

				Api.changePassword(data).then(() => {
					deferred.resolve();
				}).catch(function (errorData) {
					deferred.reject(errorData);
				});

				return deferred.promise;
			};

			Security.addExternalLogin = function (externalAccessToken, data) {
				var deferred = $q.defer();

				Api.addExternalLogin(externalAccessToken, data).then(function () {
					deferred.resolve();
				}).catch(function (errorData) {
					deferred.reject(errorData);
				});

				return deferred.promise;
			};

			function doLogin(user, data, deferred) {
				accessToken(user.data.AccessToken, data.rememberMe);
				initialize().then(() => {
					Security.redirectAuthenticated(redirectTarget() || securityProvider.urls.home);
					if (securityProvider.events.login) securityProvider.events.login(Security, user); // Your Login events
					deferred.resolve(Security.user);
				});
			}

			Security.associateExternal = function (login, returnUrl) {
				var deferred = $q.defer();
				if (securityProvider.usePopups) {
					var loginWindow = window.open(login.url, 'frame', 'resizeable,height=510,width=380');

					//Watch for close
					$timeout.cancel(externalLoginWindowTimer);
					externalLoginWindowTimer = $timeout(function closeWatcher() {
						if (!loginWindow.closed) {
							externalLoginWindowTimer = $timeout(closeWatcher, 500);
							return;
						}
						//closeOAuthWindow handler - passes external_data if there is any
						if (securityProvider.events.closeOAuthWindow) securityProvider.events.closeOAuthWindow(Security, window.external_data);

						//Return if the window was closed and external data wasn't added
						if (typeof (window.external_data) === 'undefined') {
							deferred.reject();
							return;
						}

						//Move external_data from global to local
						var external_data = window.external_data;
						delete window.external_data;

						deferred.resolve();
					}, 500);
				} else {
					localStorage.loginProvider = JSON.stringify(login);
					associating(true);
					redirectTarget(returnUrl || "/");
					window.location.href = login.url;
				}

				return deferred.promise;
			};

			Security.removeLogin = function (data) {
				var deferred = $q.defer();

				Api.removeLogin(data).then(function (result) {
					deferred.resolve(result.data);
				}).catch(function (errorData) {
					deferred.reject(errorData);
				});

				return deferred.promise;
			};

			Security.authenticate = function () {
				if (accessToken()) return;
				if (!redirectTarget()) redirectTarget($location.path());
				$location.path(securityProvider.urls.login);
			};

			Security.redirectAuthenticated = function (url) {
				if (!accessToken()) return;
				if (redirectTarget()) redirectTarget('clear');
				$location.path(url);
			};
			// Initialize
			initialize();

			return Security;
		}];
	}]);
