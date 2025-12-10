console.log('WebDemo JavaScript loaded!');

// 简单的实用函数
function showMessage(message) {
    alert('WebDemo: ' + message);
}

// 页面加载完成后执行
document.addEventListener('DOMContentLoaded', function() {
    console.log('Page loaded successfully!');
    
    // 如果有 greeting 元素,显示欢迎信息
    const greeting = document.getElementById('greeting');
    if (greeting) {
        const hour = new Date().getHours();
        let timeOfDay = '早上好';
        
        if (hour >= 12 && hour < 18) {
            timeOfDay = '下午好';
        } else if (hour >= 18) {
            timeOfDay = '晚上好';
        }
        
        greeting.textContent = timeOfDay + '! 欢迎访问 WebDemo';
    }
});
