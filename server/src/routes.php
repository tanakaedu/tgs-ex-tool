<?php
// Routes

// 接続情報の受付
// uid=学籍番号
// card=IPの下3桁
$app->post('/attend', function($request, $response, $args) {
    return "attend";
});

$app->post('/test', function($request, $response, $args) {
    return "data1=".$_POST['data1']."/data2=".$_POST['data2'];
});

$app->get('/[{name}]', function ($request, $response, $args) {
    // Sample log message
    $this->logger->info("Slim-Skeleton '/' route");

    // Render index view
    return $this->renderer->render($response, 'index.phtml', $args);
});
