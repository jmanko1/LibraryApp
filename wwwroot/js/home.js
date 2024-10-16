$.ajax({
    url: 'http://localhost:5258/users/data',
    type: 'GET',
    success: function (response) {
        console.log('Dane otrzymane:', response);
    },
    error: function (xhr, status, error) {
        document.getElementById("welcome").innerHTML = 'Nie jesteś zalogowany. <a href="users/login">Zaloguj się</a>';
    }
});
