<?php
require __DIR__ . '/../vendor/autoload.php';
require __DIR__ . '/../src/db/table-attend.php';
require __DIR__ . '/../src/db/table-class.php';
require __DIR__ . '/../src/libs/cs-attend.php';

use Am1\Attend\CsAttend;
use Am1\Attend\AttendTable;
use Am1\Attend\ClassTable;

// Instantiate the app
require __DIR__ . '/../config/config-mac.php';
$settings = require __DIR__ . '/../src/settings.php';

// Boot class
CsAttend::boot($settings['settings']);
