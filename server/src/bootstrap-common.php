<?php
require __DIR__ . '/../vendor/autoload.php';
require __DIR__ . '/db/table-attend.php';
require __DIR__ . '/db/table-class.php';
require __DIR__ . '/db/table-user.php';
require __DIR__ . '/libs/cs-attend.php';
require __DIR__ . '/libs/check-parameters.php';

use Am1\Attend\CsAttend;
use Am1\Attend\AttendTable;
use Am1\Attend\ClassTable;

// Instantiate the app
$settings = require __DIR__ . '/../src/settings.php';

// Boot class
CsAttend::boot($settings['settings']);
