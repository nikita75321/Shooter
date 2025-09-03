<?php
header("Content-Type: application/json; encoding=utf-8");

$secret_key = '6xFCXLJdDExSpd1oOeN7'; // Защищённый ключ приложения
               

$input = $_POST;

// Проверка подписи
$sig = $input['sig'];
unset($input['sig']);
ksort($input);
$str = '';
foreach ($input as $k => $v) {
  $str .= $k.'='.$v;
}

if ($sig != md5($str.$secret_key)) {
  $response['error'] = array(
    'error_code' => 10,
    'error_msg' => 'Несовпадение вычисленной и переданной подписи запроса.',
    'critical' => true
  );
} else {
  // Подпись правильная
  switch ($input['notification_type']) {
    case 'get_item':
      // Получение информации о товаре в тестовом режиме
              $item = $input['item'];
              switch ($item) {
                  case 'carrot':
                      $response['response'] = array(
                          'item_id' => 1,
                          'title' => 'Морковь',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Carrot.png',
                          'price' => 1
                      );
                      break;
                  case 'strawberry':
                      $response['response'] = array(
                          'item_id' => 2,
                          'title' => 'Клубника',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Strawberry.png',
                          'price' => 3
                      );
                      break;
                  case 'blueberry':
                      $response['response'] = array(
                          'item_id' => 3,
                          'title' => 'Черника',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Blueberry.png',
                          'price' => 7
                      );
                      break;
                  case 'lavender':
                      $response['response'] = array(
                          'item_id' => 4,
                          'title' => 'Лаванда',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Lavender.png',
                          'price' => 7
                      );
                      break;
                  case 'manuka':
                      $response['response'] = array(
                          'item_id' => 5,
                          'title' => 'Манука',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Manuka.png',
                          'price' => 5
                      );
                      break;
                  case 'orange_tulip':
                      $response['response'] = array(
                          'item_id' => 6,
                          'title' => 'Оранжевый Тюльпан',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Orange_tulip.png',
                          'price' => 2
                      );
                      break;
                  case 'rose':
                      $response['response'] = array(
                          'item_id' => 7,
                          'title' => 'Роза',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Rose.png',
                          'price' => 6
                      );
                      break;
                  case 'corn':
                      $response['response'] = array(
                          'item_id' => 8,
                          'title' => 'Кукуруза',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Corn.png',
                          'price' => 20
                      );
                      break;
                  case 'dandelion':
                      $response['response'] = array(
                          'item_id' => 9,
                          'title' => 'Одуванчик',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Dandelion.png',
                          'price' => 3
                      );
                      break;
                  case 'narcissus':
                      $response['response'] = array(
                          'item_id' => 10,
                          'title' => 'Нарцисс',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Narcissus.png',
                          'price' => 4
                      );
                      break;
                  case 'raspberry':
                      $response['response'] = array(
                          'item_id' => 11,
                          'title' => 'Малина',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Raspberry.png',
                          'price' => 26
                      );
                      break;
                  case 'tomato':
                      $response['response'] = array(
                          'item_id' => 12,
                          'title' => 'Томат',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Tomato.png',
                          'price' => 12
                      );
                      break;
                  case 'apple':
                      $response['response'] = array(
                          'item_id' => 13,
                          'title' => 'Яблоко',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Apple.png',
                          'price' => 55
                      );
                      break;
                  case 'bamboo':
                      $response['response'] = array(
                          'item_id' => 14,
                          'title' => 'Бамбук',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Bamboo.png',
                          'price' => 29
                      );
                      break;
                  case 'lilac':
                      $response['response'] = array(
                          'item_id' => 15,
                          'title' => 'Сирень',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Lilac.png',
                          'price' => 32
                      );
                      break;
                  case 'lumira':
                      $response['response'] = array(
                          'item_id' => 16,
                          'title' => 'Люмира',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Lumira.png',
                          'price' => 43
                      );
                      break;
                  case 'pumpkin':
                      $response['response'] = array(
                          'item_id' => 17,
                          'title' => 'Тыква',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Pumpkin.png',
                          'price' => 30
                      );
                      break;
                  case 'watermelon':
                      $response['response'] = array(
                          'item_id' => 18,
                          'title' => 'Арбуз',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Watermelon.png',
                          'price' => 28
                      );
                      break;
                  case 'cactus':
                      $response['response'] = array(
                          'item_id' => 19,
                          'title' => 'Кактус',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Cactus.png',
                          'price' => 71
                      );
                      break;
                  case 'coconut':
                      $response['response'] = array(
                          'item_id' => 20,
                          'title' => 'Кокос',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Coconut.png',
                          'price' => 63
                      );
                      break;
                  case 'dragon_fruit':
                      $response['response'] = array(
                          'item_id' => 21,
                          'title' => 'Драгон фрукт',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Dragon_fruit.png',
                          'price' => 112
                      );
                      break;
                  case 'mango':
                      $response['response'] = array(
                          'item_id' => 22,
                          'title' => 'Манго',
                          'photo_url' => 'https://www.growagardenoffline.online/images/Mango.png',
                          'price' => 83
                      );
                      break;
                  default:
                      $response['error'] = array(
                          'error_code' => 20,
                          'error_msg' => 'Товара не существует.',
                          'critical' => true
                      );
              }
              break;

    case 'get_item_test':
        // Получение информации о товаре в тестовом режиме
        $item = $input['item'];
        switch ($item) {
            case 'carrot':
                $response['response'] = array(
                    'item_id' => 1,
                    'title' => 'Морковь',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Carrot.png',
                    'price' => 1
                );
                break;
            case 'strawberry':
                $response['response'] = array(
                    'item_id' => 2,
                    'title' => 'Клубника',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Strawberry.png',
                    'price' => 3
                );
                break;
            case 'blueberry':
                $response['response'] = array(
                    'item_id' => 3,
                    'title' => 'Черника',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Blueberry.png',
                    'price' => 7
                );
                break;
            case 'lavender':
                $response['response'] = array(
                    'item_id' => 4,
                    'title' => 'Лаванда',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Lavender.png',
                    'price' => 7
                );
                break;
            case 'manuka':
                $response['response'] = array(
                    'item_id' => 5,
                    'title' => 'Манука',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Manuka.png',
                    'price' => 5
                );
                break;
            case 'orange_tulip':
                $response['response'] = array(
                    'item_id' => 6,
                    'title' => 'Оранжевый Тюльпан',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Orange_tulip.png',
                    'price' => 2
                );
                break;
            case 'rose':
                $response['response'] = array(
                    'item_id' => 7,
                    'title' => 'Роза',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Rose.png',
                    'price' => 6
                );
                break;
            case 'corn':
                $response['response'] = array(
                    'item_id' => 8,
                    'title' => 'Кукуруза',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Corn.png',
                    'price' => 20
                );
                break;
            case 'dandelion':
                $response['response'] = array(
                    'item_id' => 9,
                    'title' => 'Одуванчик',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Dandelion.png',
                    'price' => 3
                );
                break;
            case 'narcissus':
                $response['response'] = array(
                    'item_id' => 10,
                    'title' => 'Нарцисс',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Narcissus.png',
                    'price' => 4
                );
                break;
            case 'raspberry':
                $response['response'] = array(
                    'item_id' => 11,
                    'title' => 'Малина',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Raspberry.png',
                    'price' => 26
                );
                break;
            case 'tomato':
                $response['response'] = array(
                    'item_id' => 12,
                    'title' => 'Томат',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Tomato.png',
                    'price' => 12
                );
                break;
            case 'apple':
                $response['response'] = array(
                    'item_id' => 13,
                    'title' => 'Яблоко',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Apple.png',
                    'price' => 55
                );
                break;
            case 'bamboo':
                $response['response'] = array(
                    'item_id' => 14,
                    'title' => 'Бамбук',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Bamboo.png',
                    'price' => 29
                );
                break;
            case 'lilac':
                $response['response'] = array(
                    'item_id' => 15,
                    'title' => 'Сирень',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Lilac.png',
                    'price' => 32
                );
                break;
            case 'lumira':
                $response['response'] = array(
                    'item_id' => 16,
                    'title' => 'Люмира',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Lumira.png',
                    'price' => 43
                );
                break;
            case 'pumpkin':
                $response['response'] = array(
                    'item_id' => 17,
                    'title' => 'Тыква',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Pumpkin.png',
                    'price' => 30
                );
                break;
            case 'watermelon':
                $response['response'] = array(
                    'item_id' => 18,
                    'title' => 'Арбуз',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Watermelon.png',
                    'price' => 28
                );
                break;
            case 'cactus':
                $response['response'] = array(
                    'item_id' => 19,
                    'title' => 'Кактус',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Cactus.png',
                    'price' => 71
                );
                break;
            case 'coconut':
                $response['response'] = array(
                    'item_id' => 20,
                    'title' => 'Кокос',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Coconut.png',
                    'price' => 63
                );
                break;
            case 'dragon_fruit':
                $response['response'] = array(
                    'item_id' => 21,
                    'title' => 'Драгон фрукт',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Dragon_fruit.png',
                    'price' => 112
                );
                break;
            case 'mango':
                $response['response'] = array(
                    'item_id' => 22,
                    'title' => 'Манго',
                    'photo_url' => 'https://www.growagardenoffline.online/images/Mango.png',
                    'price' => 83
                );
                break;
            default:
                $response['error'] = array(
                    'error_code' => 20,
                    'error_msg' => 'Товара не существует.',
                    'critical' => true
                );
        }
        break;

    case 'order_status_change':
      // Изменение статуса заказа
      if ($input['status'] == 'chargeable') {
        $order_id = intval($input['order_id']);

        // Код проверки товара, включая его стоимость
        $app_order_id = 1; // Получающийся у вас идентификатор заказа.

        $response['response'] = array(
          'order_id' => $order_id,
          'app_order_id' => $app_order_id,
        );
      } else {
        $response['error'] = array(
          'error_code' => 100,
          'error_msg' => 'Передано непонятно что вместо chargeable.',
          'critical' => true
        );
      }
      break;

    case 'order_status_change_test':
      // Изменение статуса заказа в тестовом режиме
      if ($input['status'] == 'chargeable') {
        $order_id = intval($input['order_id']);

        $app_order_id = 1; // Тут фактического заказа может не быть — тестовый режим.

        $response['response'] = array(
          'order_id' => $order_id,
          'app_order_id' => $app_order_id,
        );
      } else {
        $response['error'] = array(
          'error_code' => 100,
          'error_msg' => 'Передано непонятно что вместо chargeable.',
          'critical' => true
        );
      }
      break;
  }
}

echo json_encode($response);
?>
