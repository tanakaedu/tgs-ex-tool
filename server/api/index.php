<?php

/*
foreach($_SERVER as $k => $v) {
    echo "$k = $v <br/>";
}*/

if (PHP_SAPI == 'cli-server') {
    // To help the built-in PHP dev server, check if the request was actually for
    // something which should probably be served as a static file
    $file = __DIR__ . $_SERVER['REQUEST_URI'];
    if (is_file($file)) {
        return false;
    }

    require __DIR__ . '/../src/bootstrap-mac.php';
}
else {
    require __DIR__ . '/../src/bootstrap-sakura.php';
}

session_start();

// Instantiate the app
$app = new \Slim\App($settings);

// Set up dependencies
require __DIR__ . '/../src/dependencies.php';

// Register middleware
require __DIR__ . '/../src/middleware.php';

// Register routes
require __DIR__ . '/../src/routes.php';

// Run app
$app->run();
