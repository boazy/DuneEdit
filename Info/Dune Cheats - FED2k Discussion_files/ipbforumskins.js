// Created by ipbforumskins.com
// You do not have permission to copy code or use this file without permission from IPBForumSkins.com

jQuery.noConflict();

jQuery(document).ready(function($){

	$('a[href=#top], a[href=#ipboard_body]').click(function(){
		$('html, body').animate({scrollTop:0}, 400);
        return false;
	});
	
	$(".forum_name").hover(function() {
		$(this).next(".forum_desc_pos").children(".forum_desc_con").stop()
		.animate({left: "0", opacity:1}, "fast")
		.css("display","block")
	}, function() {
		$(this).next(".forum_desc_pos").children(".forum_desc_con").stop()
		.animate({left: "10", opacity: 0}, "fast", function(){
			$(this).hide();
		})
	});
	
	$('#topicViewBasic').click(function(){
		$(this).addClass("active");
		$('#topicViewRegular').removeClass("active");
		$("#customize_topic").addClass("basicTopicView");
		$.cookie('ctv','basic',{ expires: 365, path: '/'});
		return false;
	});
	
	$('#topicViewRegular').click(function(){
		$(this).addClass("active");
		$('#topicViewBasic').removeClass("active");
		$("#customize_topic").removeClass("basicTopicView");
		$.cookie('ctv',null,{ expires: -1, path: '/'});
		return false;
	});
	
	if ( ($.cookie('ctv') != null))	{
		$("#customize_topic").addClass("basicTopicView");
		$("#topicViewBasic").addClass("active");
	}
	else{
		$("#topicViewRegular").addClass("active");
	}
	
	var customElements = ".customcolor, .maintitle, #primary_nav, .col_c_icon img, #search .submit_input, #board_stats .value, #secondary_navigation, .topic_buttons li a, .pagination .pages li.active, #bottomScroll, .post_controls a, #primary_extra_menucontent, #more_apps_menucontent, .ipsButton";
	var customText = "";
	
	$('#colorpicker').ColorPicker({
		onSubmit: function(hsb, hex, rgb, el) {
			$(el).val(hex);
			$(el).ColorPickerHide();
			$(el).css("backgroundColor", "#" + hex);
			$(customElements).css("background-color", "#" + hex);
			$(customText).css("color", "#" + hex);
			$.cookie('customcolor',hex,{ expires: 365, path: '/'});
		},
		onBeforeShow: function () {
			$(this).ColorPickerSetColor(this.value);
		},
		onChange: function (hsb, hex, rgb) {
			$(customElements).css("background-color", "#" + hex);
			$(customText).css("color", "#" + hex);
			$.cookie('customcolor',hex,{ expires: 365, path: '/'});
		}
	})
	.bind('keyup', function(){
		$(this).ColorPickerSetColor(this.value);
	});

	if ( ($.cookie('customcolor') != null))	{
		$(customElements).css("background-color", "#" + $.cookie('customcolor'));
		$(customText).css("color", "#" + $.cookie('customcolor'));
		$("#colorpicker").val($.cookie('customcolor'));
	}
	else{
		$(customElements).css("background-color","#3a85ba");
		$(customText).css("color","#3a85ba");
	}

});