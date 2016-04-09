<?php
// Routes

use Am1\Attend\CheckParameters;
use Am1\Attend\CsAttend;

// 接続情報の受付
// uid=学籍番号
// card=IPの下3桁
// 結果はJSONで返信する(message=文字列/server_ip=ローカルサーバーのIP/name=学生名)
$app->post('/attend', function ($request, $response, $args) {
    // 名前の確認。失敗時はnameに空文字を返す
    return $response->write(
        json_encode(
            [
                'message' => 'ok',
                'name' => CsAttend::getName(),
                'server_ip' => CsAttend::getServerIP()
            ]
        )
    );

    // uidの有効性チェック。失敗時は400(Bad Request)
    // カード番号の有効性チェック。失敗時は400(Bad Request)
})->add(new CheckParameters());

$app->post('/test', function ($request, $response, $args) {
    return 'data1='.$_POST['data1'].'/data2='.$_POST['data2'];
});

$app->get('/[{name}]', function ($request, $response, $args) {
    // Sample log message
    $this->logger->info("Slim-Skeleton '/' route");

    // Render index view
    return $this->renderer->render($response, 'index.phtml', $args);
});
